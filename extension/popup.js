var VI = /^vi/i.test(chrome.i18n.getUILanguage());
function t(vi, en){ return VI ? vi : en; }
if (!VI) {
  document.documentElement.lang = 'en';
  document.getElementById('t1').textContent = 'DeskPanel · AI usage limits';
  document.getElementById('t2').textContent = 'DeskPanel port';
  document.getElementById('t3').textContent = 'The extension uses the claude.ai / chatgpt.com sessions you are signed in to in this browser. Every 3 minutes it reads the limit % and sends it only to DeskPanel on this PC (localhost). It does not read conversations and sends nothing anywhere else.';
}
var SEND = t('Gửi ngay', 'Send now');
document.getElementById('now').textContent = SEND;
function st(code){
  if (code === 200) return [t('Đã kết nối', 'Connected'), 'ok'];
  if (code === 401 || code === 403) return [t('Chưa đăng nhập trên trình duyệt này', 'Not signed in in this browser'), 'bad'];
  if (code === 0) return [t('Lỗi mạng', 'Network error'), 'bad'];
  return [t('Lỗi ', 'Error ') + code, 'bad'];
}
function show(l){
  if (!l) return;
  [['c', l.claude], ['g', l.chatgpt]].forEach(function(x){ var s = st(x[1]), e = document.getElementById(x[0]); e.textContent = s[0]; e.className = s[1]; });
  var d = document.getElementById('d');
  d.textContent = l.sent ? t('Đã gửi ', 'Sent ') + new Date(l.at).toLocaleTimeString() : (l.err || t('Chưa gửi', 'Not sent yet')); d.className = l.sent ? 'ok' : 'bad';
}
chrome.storage.local.get(['last', 'port'], function(v){ show(v.last); document.getElementById('port').value = v.port || 8765; });
document.getElementById('now').onclick = function(){
  this.textContent = t('Đang gửi…', 'Sending…');
  var b = this;
  chrome.runtime.sendMessage('sync', function(){ chrome.storage.local.get('last', function(v){ show(v.last); b.textContent = SEND; }); });
};
document.getElementById('port').onchange = function(){ var p = parseInt(this.value, 10); if (p > 0) chrome.storage.local.set({ port: p }); };
