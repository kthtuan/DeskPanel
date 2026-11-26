function st(code){
  if (code === 200) return ['Đã kết nối', 'ok'];
  if (code === 401 || code === 403) return ['Chưa đăng nhập trên trình duyệt này', 'bad'];
  if (code === 0) return ['Lỗi mạng', 'bad'];
  return ['Lỗi ' + code, 'bad'];
}
function show(l){
  if (!l) return;
  [['c', l.claude], ['g', l.chatgpt]].forEach(function(x){ var s = st(x[1]), e = document.getElementById(x[0]); e.textContent = s[0]; e.className = s[1]; });
  var d = document.getElementById('d');
  d.textContent = l.sent ? 'Đã gửi ' + new Date(l.at).toLocaleTimeString() : (l.err || 'Chưa gửi'); d.className = l.sent ? 'ok' : 'bad';
}
chrome.storage.local.get(['last', 'port'], function(v){ show(v.last); document.getElementById('port').value = v.port || 8765; });
document.getElementById('now').onclick = function(){
  this.textContent = 'Đang gửi…';
  var b = this;
  chrome.runtime.sendMessage('sync', function(){ chrome.storage.local.get('last', function(v){ show(v.last); b.textContent = 'Gửi ngay'; }); });
};
document.getElementById('port').onchange = function(){ var p = parseInt(this.value, 10); if (p > 0) chrome.storage.local.set({ port: p }); };
