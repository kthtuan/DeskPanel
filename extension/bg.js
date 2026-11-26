// DeskPanel AI: 3 phut mot lan doc % gioi han tu claude.ai va chatgpt.com (bang phien dang nhap san co
// cua trinh duyet) roi gui ve DeskPanel tren chinh may nay (http://localhost:<cong>). Khong gui di dau khac.
const PERIOD_MIN = 3;

async function claude() {
  try {
    const r = await fetch('https://claude.ai/api/organizations', { credentials: 'include' });
    if (!r.ok) return { status: r.status };
    const orgs = await r.json();
    const o = (orgs || []).find(x => (x.capabilities || []).includes('chat')) || (orgs || [])[0];
    if (!o) return { status: 404 };
    const u = await fetch('https://claude.ai/api/organizations/' + o.uuid + '/usage', { credentials: 'include' });
    return { status: u.status, tier: o.rate_limit_tier || '', caps: o.capabilities || [], usage: u.ok ? await u.json() : null };
  } catch (e) { return { status: 0, error: String(e) }; }
}

async function chatgpt() {
  try {
    const sr = await fetch('https://chatgpt.com/api/auth/session', { credentials: 'include' });
    if (!sr.ok) return { status: sr.status };
    const s = await sr.json();
    if (!s || !s.accessToken) return { status: 401 };
    const h = { Authorization: 'Bearer ' + s.accessToken };
    if (s.account && s.account.id) h['ChatGPT-Account-Id'] = s.account.id;
    const r = await fetch('https://chatgpt.com/backend-api/wham/usage', { headers: h, credentials: 'include' });
    return { status: r.status, plan: (s.account && s.account.planType) || '', usage: r.ok ? await r.json() : null };
  } catch (e) { return { status: 0, error: String(e) }; }
}

async function sync() {
  const { port = 8765 } = await chrome.storage.local.get('port');
  const data = { claude: await claude(), chatgpt: await chatgpt() };
  let sent = false, err = '';
  try {
    const r = await fetch('http://localhost:' + port + '/api/ai/push', {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'X-DeskPanel': '1' }, body: JSON.stringify(data),
    });
    sent = r.ok; if (!r.ok) err = 'DeskPanel trả lỗi ' + r.status;
  } catch (e) { err = 'Không thấy DeskPanel ở cổng ' + port; }
  await chrome.storage.local.set({ last: { at: Date.now(), claude: data.claude.status, chatgpt: data.chatgpt.status, sent, err } });
  return { sent, err, claude: data.claude.status, chatgpt: data.chatgpt.status };
}

chrome.runtime.onInstalled.addListener(() => { chrome.alarms.create('sync', { periodInMinutes: PERIOD_MIN }); sync(); });
chrome.runtime.onStartup.addListener(() => { chrome.alarms.create('sync', { periodInMinutes: PERIOD_MIN }); sync(); });
chrome.alarms.onAlarm.addListener(a => { if (a.name === 'sync') sync(); });
chrome.runtime.onMessage.addListener((m, _s, reply) => { if (m === 'sync') { sync().then(reply); return true; } });
