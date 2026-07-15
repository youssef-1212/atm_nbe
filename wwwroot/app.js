/* ================= DATA ================= */
let ATMS = [];

async function fetchATMs() {
  try {
    const response = await fetch('/api/Atms');
    ATMS = await response.json();
  } catch (error) {
    console.error("خطأ أثناء جلب بيانات أجهزة الصراف الآلي:", error);
  }
}

/* مستوى الازدحام يُشتق من إشارة حية — هنا: عدد العمليات المنفَّذة خلال
   آخر 15 دقيقة على جهاز الصراف الآلي. في بيئة الإنتاج تُستمد هذه البيانات من
   سجلّات نظام المراقبة الفعلي وليس من بيانات ثابتة كما هو الحال هنا.
   عدم تسجيل أي عملية خلال آخر دقائق يُعدّ مؤشراً على الازدحام، إذ يدل
   على تعطّل تدفّق المعاملات رغم استمرار عمل جهاز الصراف الآلي. */
function crowdInfo(a){
  const n = a.tx15;
  if(a.status==='err') return {label:"غير متاح", note:"جهاز الصراف الآلي خارج الخدمة حالياً", level:"err"};
  if(n === 0) return {label:"مرتفع", note:"لم تُسجَّل أي عملية خلال آخر 15 دقيقة، مما قد يشير إلى وجود ازدحام على جهاز الصراف الآلي", level:"err"};
  if(n <= 10) return {label:"منخفض", note:`سُجِّلت ${n} عمليات خلال آخر 15 دقيقة، ويعمل جهاز الصراف الآلي بانتظام`, level:"ok"};
  return {label:"متوسط", note:`سُجِّلت ${n} عملية خلال آخر 15 دقيقة`, level:"warn"};
}

const STATUS_LABEL = {ok:"متاح", warn:"مزدحم / رصيد نقدي منخفض", err:"خارج الخدمة"};
const STATUS_CLASS = {ok:"status-ok", warn:"status-warn", err:"status-err"};
const SERVICE_LABEL = {withdraw:"سحب نقدي", deposit:"إيداع نقدي", transfer:"تحويل أموال"};
const SERVICE_ICON  = {withdraw:"ti-cash", deposit:"ti-arrow-down-circle", transfer:"ti-arrows-exchange"};

let selectedServices = new Set();
let userLocation = null;
let map;
let markers = [];

function haversine(lat1, lng1, lat2, lng2){
  const R = 6371;
  const dLat = (lat2-lat1) * Math.PI/180;
  const dLng = (lng2-lng1) * Math.PI/180;
  const a = Math.sin(dLat/2)**2 + Math.cos(lat1*Math.PI/180)*Math.cos(lat2*Math.PI/180)*Math.sin(dLng/2)**2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
}

function setLocationCard(state, html){
  const el = document.getElementById('locate-status');
  el.className = `location-card ${state}`;
  el.innerHTML = html;
}

function useMyLocation(){
  setLocationCard('loading', '<i class="ti ti-loader-2"></i> جارٍ تحديد موقعكم...');
  if(!navigator.geolocation || !window.isSecureContext){
    locateByIP('تحديد الموقع الدقيق غير متاح هنا (يتطلب اتصالاً آمناً HTTPS)');
    return;
  }
  navigator.geolocation.getCurrentPosition(
    pos=>{
      setUserLocation(pos.coords.latitude, pos.coords.longitude);
      setLocationCard('ok', '<i class="ti ti-map-pin-check"></i> تم تحديد موقعكم — أجهزة الصراف الآلي مرتبة حسب الأقرب');
    },
    err=>{
      let reason = 'حدث خطأ غير متوقع';
      if(err.code === err.PERMISSION_DENIED) reason = 'تم رفض إذن الوصول للموقع';
      else if(err.code === err.POSITION_UNAVAILABLE) reason = 'تعذر تحديد موقعكم بدقة';
      else if(err.code === err.TIMEOUT) reason = 'انتهت مهلة تحديد الموقع';
      locateByIP(reason);
    },
    {enableHighAccuracy:true, timeout:8000}
  );
}

/* محاولة تحديد الموقع تلقائياً عن طريق عنوان الـ IP (تقريبي، ولا يتطلب
   إذناً من المتصفح) كبديل عندما يتعذر تحديد الموقع الدقيق عبر GPS. وإذا
   فشلت هذه المحاولة أيضاً، يُقترح على المستخدم النقر على الخريطة يدوياً. */
function locateByIP(reason){
  setLocationCard('loading', `<i class="ti ti-loader-2"></i> ${reason} — جارٍ تحديد موقعكم تقريبياً...`);
  fetch('https://ipapi.co/json/')
    .then(r=>r.json())
    .then(data=>{
      if(data && data.latitude && data.longitude){
        setUserLocation(data.latitude, data.longitude);
        setLocationCard('approx', `<i class="ti ti-map-pin-cog"></i> تم تحديد موقعكم بشكل تقريبي${data.city ? ' ('+data.city+')' : ''} — <a onclick="useMyLocation()">إعادة المحاولة بدقة</a> أو انقروا على الخريطة لتعديله`);
      } else {
        setLocationCard('err', `<i class="ti ti-map-pin-off"></i> ${reason} — انقروا على أي نقطة في الخريطة لتحديد موقعكم`);
      }
    })
    .catch(()=>{
      setLocationCard('err', `<i class="ti ti-map-pin-off"></i> ${reason} — انقروا على أي نقطة في الخريطة لتحديد موقعكم`);
    });
}

/* تحديد الموقع يدوياً (كبديل عند رفض إذن المتصفح أو فتح الصفحة بدون
   بروتوكول HTTPS الآمن، وهي الحالات التي يتوقف فيها محدد المواقع عن العمل) */
function setUserLocation(lat, lng){
  userLocation = {lat, lng};
  if(map){
    if(window._userMarker) map.removeLayer(window._userMarker);
    window._userMarker = L.circleMarker([lat, lng], {
      radius:8, color:'#fff', weight:2, fillColor:'#107614', fillOpacity:1
    }).addTo(map).bindTooltip('موقعكم الحالي');
    map.setView([lat, lng], 13);
  }
  renderList();
}

/* ================= CUSTOMER: LIST + MAP ================= */
function renderChips(){
  const chips = [
    {key:"withdraw", label:"سحب نقدي"},
    {key:"deposit", label:"إيداع نقدي"},
    {key:"transfer", label:"تحويل أموال"},
  ];
  document.getElementById('filter-chips').innerHTML = chips.map(c=>
    `<button class="chip ${selectedServices.has(c.key)?'selected':''}" onclick="toggleFilter('${c.key}')">${c.label}</button>`
  ).join('');
}
function toggleFilter(key){
  if(selectedServices.has(key)) selectedServices.delete(key);
  else selectedServices.add(key);
  renderChips();
  renderList();
}

function renderList(){
  const q = document.getElementById('search-input').value.trim();
  let filtered = ATMS.filter(a=>{
    const isWorking = a.status !== 'err';
    const matchesQuery = !q || a.name.includes(q) || a.address.includes(q);
    const matchesServices = selectedServices.size===0 || [...selectedServices].every(s=>a.services.includes(s));
    return isWorking && matchesQuery && matchesServices;
  });

  if(userLocation){
    filtered = filtered.map(a=>({...a, dist:haversine(userLocation.lat, userLocation.lng, a.lat, a.lng)}));
    filtered.sort((x,y)=>x.dist-y.dist);
  }

  document.getElementById('atm-list').innerHTML = filtered.map(a=>`
    <div class="atm-card ${a.status==='err'?'disabled':''}" onclick="openDrawer(${a.id})">
      <div class="atm-card-top">
        <div>
          <div class="atm-name">${a.name}</div>
          <div class="atm-meta">${a.dist!==undefined ? a.dist.toFixed(1)+' كم — ' : ''}${a.address}</div>
        </div>
        <span class="status-badge ${STATUS_CLASS[a.status]}"><span class="dot"></span>${STATUS_LABEL[a.status]}</span>
      </div>
      <div class="atm-services">
        ${a.services.map(s=>`<span><i class="ti ${SERVICE_ICON[s]}"></i>${SERVICE_LABEL[s]}</span>`).join('') || '<span>لا تتوفر خدمات حالياً</span>'}
      </div>
    </div>
  `).join('');

  if(filtered.length===0){
    document.getElementById('atm-list').innerHTML = renderEmptyStateWithNearest();
  }
}

/* عند عدم وجود نتائج مطابقة للتصفية، يُعرض في الجزء الفارغ من الشاشة
   أقرب صراف آلي أو فرع لموقع العميل بدلاً من ترك المساحة فارغة. */
function renderEmptyStateWithNearest(){
  if(!userLocation){
    return `
      <div style="text-align:center; color:var(--text-muted); font-size:13px; padding:30px 10px 14px;">لا توجد نتائج مطابقة</div>
      <div style="text-align:center; padding:0 10px 30px;">
        <button class="btn-primary" style="width:auto; padding:9px 18px; display:inline-flex;" onclick="useMyLocation()">
          <i class="ti ti-current-location"></i> عرض أقرب صراف آلي لموقعي
        </button>
      </div>`;
  }
  const nearest = ATMS
    .filter(a=>a.status !== 'err')
    .map(a=>({...a, dist:haversine(userLocation.lat, userLocation.lng, a.lat, a.lng)}))
    .sort((x,y)=>x.dist-y.dist)[0];
  if(!nearest) return `<div style="text-align:center; color:var(--text-muted); font-size:13px; padding:40px 10px;">لا توجد نتائج مطابقة</div>`;
  return `
    <div style="text-align:center; color:var(--text-muted); font-size:12.5px; padding:18px 10px 8px;">لا توجد نتائج مطابقة للتصفية، ولكن أقرب صراف آلي لموقعكم هو:</div>
    <div class="atm-card" onclick="openDrawer(${nearest.id})">
      <div class="atm-card-top">
        <div>
          <div class="atm-name">${nearest.name}</div>
          <div class="atm-meta">${nearest.dist.toFixed(1)} كم — ${nearest.address}</div>
        </div>
        <span class="status-badge ${STATUS_CLASS[nearest.status]}"><span class="dot"></span>${STATUS_LABEL[nearest.status]}</span>
      </div>
      <div class="atm-services">
        ${nearest.services.map(s=>`<span><i class="ti ${SERVICE_ICON[s]}"></i>${SERVICE_LABEL[s]}</span>`).join('') || '<span>لا تتوفر خدمات حالياً</span>'}
      </div>
    </div>`;
}

function initMap(){
  map = L.map('map', {zoomControl:true}).setView([30.075, 31.335], 13);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution:'&copy; OpenStreetMap contributors', maxZoom:19
  }).addTo(map);
  const colorFor = {ok:'#639922', warn:'#EF9F27', err:'#E24B4A'};
  ATMS.filter(a=>a.status !== 'err').forEach(a=>{
    const marker = L.circleMarker([a.lat, a.lng], {
      radius:9, color:'#fff', weight:2, fillColor:colorFor[a.status], fillOpacity:1
    }).addTo(map);
    marker.bindTooltip(a.name, {direction:'top'});
    marker.on('click', ()=>openDrawer(a.id));
  });
  map.on('click', e=>{
    setUserLocation(e.latlng.lat, e.latlng.lng);
    setLocationCard('ok', '<i class="ti ti-map-pin-check"></i> تم تحديد موقعكم من الخريطة — أجهزة الصراف الآلي مرتبة حسب الأقرب');
  });
}

let currentDrawerId = null;

function openDrawer(id){
  currentDrawerId = id;
  const a = ATMS.find(x=>x.id===id);
  const crowd = crowdInfo(a);
  document.getElementById('drawer-name').textContent = a.name;
  document.getElementById('drawer-address').textContent = a.address;
  document.getElementById('drawer-status').innerHTML =
    `<span class="status-badge ${STATUS_CLASS[a.status]}"><span class="dot"></span>${STATUS_LABEL[a.status]}</span>`;
  document.getElementById('drawer-services').innerHTML = a.services.length
    ? a.services.map(s=>`<div class="service-row"><i class="ti ${SERVICE_ICON[s]}"></i>${SERVICE_LABEL[s]}</div>`).join('')
    : `<div class="service-row" style="color:var(--text-muted)">لا تتوفر خدمات حالياً</div>`;
  document.getElementById('drawer-denoms').innerHTML = a.denoms && a.denoms.length
    ? `<div style="display:flex; flex-wrap:wrap; gap:6px;">${a.denoms.map(d=>`<span style="font-size:12.5px; padding:5px 10px; border:1px solid var(--border); border-radius:8px; color:var(--text);">${d} جنيه</span>`).join('')}</div>`
    : `<div style="font-size:12.5px; color:var(--text-muted)">لا تتوفر بيانات الفئات حالياً</div>`;
  document.getElementById('drawer-crowd-badge').innerHTML =
    `<span class="status-badge ${STATUS_CLASS[crowd.level]}"><span class="dot"></span>${crowd.label}</span>`;
  document.getElementById('drawer-crowd').textContent = crowd.note;
  document.getElementById('drawer-directions').href = `https://www.google.com/maps/dir/?api=1&destination=${a.lat},${a.lng}`;
  document.getElementById('report-confirm').style.display = 'none';
  document.getElementById('drawer-overlay').classList.add('open');
  document.getElementById('drawer').classList.add('open');
}

/* ================= REPORT ISSUE MODAL ================= */
const REPORT_CATEGORIES = [
  {id:"power",  label:"انقطاع التيار الكهربائي عن جهاز الصراف الآلي"},
  {id:"cash",   label:"نفاد الرصيد النقدي من جهاز الصراف الآلي"},
  {id:"card",   label:"احتجاز البطاقة داخل جهاز الصراف الآلي"},
  {id:"screen", label:"عطل في الشاشة أو لوحة المفاتيح"},
  {id:"other",  label:"مشكلة أخرى"},
];

let selectedCategory = null;

function isValidNationalId(id){
  if(!/^\d{14}$/.test(id)) return false;
  const century = id[0];
  if(century !== '2' && century !== '3') return false;
  const month = parseInt(id.substr(3,2), 10);
  const day = parseInt(id.substr(5,2), 10);
  const gov = parseInt(id.substr(7,2), 10);
  if(month < 1 || month > 12) return false;
  if(day < 1 || day > 31) return false;
  if(gov < 1 || gov > 88) return false;
  return true;
}

function buildCategorySelect(){
  const select = document.getElementById('report-category-select');
  select.innerHTML = `<option value="" selected disabled>اختر نوع المشكلة</option>` +
    REPORT_CATEGORIES.map(c => `<option value="${c.id}">${c.label}</option>`).join('');
}

function onCategoryChange(){
  selectedCategory = document.getElementById('report-category-select').value || null;
  document.getElementById('category-error').classList.remove('show');
}

function openReportModal(){
  if(currentDrawerId==null) return;
  selectedCategory = null;
  buildCategorySelect();
  document.getElementById('report-description').value = '';
  document.getElementById('national-id').value = '';
  ['category-error','description-error','national-id-error'].forEach(id=>{
    document.getElementById(id).classList.remove('show');
  });
  document.getElementById('report-modal-overlay').classList.add('open');
}

function closeReportModal(){
  document.getElementById('report-modal-overlay').classList.remove('open');
}

function submitReport(){
  if(currentDrawerId==null) return;
  let valid = true;

  if(!selectedCategory){
    document.getElementById('category-error').classList.add('show');
    valid = false;
  }

  const description = document.getElementById('report-description').value.trim();
  if(description.length < 5){
    document.getElementById('description-error').classList.add('show');
    valid = false;
  } else {
    document.getElementById('description-error').classList.remove('show');
  }

  const nationalId = document.getElementById('national-id').value.trim();
  if(!isValidNationalId(nationalId)){
    document.getElementById('national-id-error').classList.add('show');
    valid = false;
  } else {
    document.getElementById('national-id-error').classList.remove('show');
  }

  if(!valid) return;

  const reportPayload = {
    AtmId: currentDrawerId,
    Category: selectedCategory,
    Description: description,
    NationalId: nationalId
  };

  fetch('/api/Atms/report', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(reportPayload)
  })
  .then(res => {
    if (!res.ok) throw new Error('Failed to submit report');
    return res.json();
  })
  .then(() => {
    const a = ATMS.find(x=>x.id===currentDrawerId);
    a.reported = true;
    a.reportedAt = new Date();
    a.reportCategory = selectedCategory;
    a.reportDescription = description;
    a.reportNationalId = nationalId;

    closeReportModal();
    document.getElementById('report-confirm').style.display = 'block';
  })
  .catch(err => {
    console.error(err);
    alert('حدث خطأ أثناء إرسال البلاغ. يرجى المحاولة مرة أخرى.');
  });
}

function closeDrawer(){
  document.getElementById('drawer-overlay').classList.remove('open');
  document.getElementById('drawer').classList.remove('open');
}

let routePolyline = null;
let routeAtmMarker = null;

async function findAtmOnRoute() {
  const destInput = document.getElementById('destination-input').value.trim();
  if (!destInput) {
    alert("يرجى إدخال وجهتكم أولاً.");
    return;
  }
  if (!userLocation) {
    alert("يرجى تحديد موقعكم الحالي أو النقر على الخريطة لحساب المسار.");
    return;
  }

  const origin = `${userLocation.lat},${userLocation.lng}`;
  
  try {
    setLocationCard('loading', '<i class="ti ti-loader-2"></i> جارٍ حساب المسار وأقرب صراف آلي...');
    const res = await fetch(`/api/Atms/route-atm?origin=${encodeURIComponent(origin)}&destination=${encodeURIComponent(destInput)}`);
    if (!res.ok) throw new Error('تعذر العثور على مسار.');
    
    const data = await res.json();
    
    clearRoute(false);
    
    if (map && data.route && data.route.length > 0) {
      routePolyline = L.polyline(data.route, {color: '#107614', weight: 5, opacity: 0.7}).addTo(map);
      map.fitBounds(routePolyline.getBounds());
    }
    
    if (data.nearestAtm) {
      setLocationCard('ok', `<i class="ti ti-map-pin-check"></i> أقرب صراف آلي على المسار هو: <b>${data.nearestAtm.name}</b> (على بعد ${data.distanceToRouteKm.toFixed(2)} كم من المسار)`);
      
      openDrawer(data.nearestAtm.id);
      
      if (map) {
        routeAtmMarker = L.circleMarker([data.nearestAtm.lat, data.nearestAtm.lng], {
          radius: 14, color: '#FAB40C', weight: 4, fillColor: '#639922', fillOpacity: 0.9
        }).addTo(map).bindTooltip("أقرب صراف آلي على مساركم").openTooltip();
      }
    } else {
      setLocationCard('approx', '<i class="ti ti-info-circle"></i> تم العثور على المسار ولكن لا توجد أجهزة صراف آلي قريبة منه.');
    }
    
    document.getElementById('clear-route-btn').style.display = 'block';
  } catch (error) {
    console.error(error);
    setLocationCard('err', '<i class="ti ti-map-pin-off"></i> فشل حساب المسار. يرجى التحقق من إدخال وجهة صحيحة.');
  }
}

function clearRoute(resetCard = true) {
  if (map) {
    if (routePolyline) {
      map.removeLayer(routePolyline);
      routePolyline = null;
    }
    if (routeAtmMarker) {
      map.removeLayer(routeAtmMarker);
      routeAtmMarker = null;
    }
  }
  document.getElementById('clear-route-btn').style.display = 'none';
  if (resetCard) {
    setLocationCard('ok', '<i class="ti ti-map-pin-check"></i> تم تحديد موقعكم — أجهزة الصراف الآلي مرتبة حسب الأقرب');
  }
}

/* ================= INIT ================= */
async function initApp() {
  await fetchATMs();
  renderChips();
  renderList();
  setLocationCard('loading', '<i class="ti ti-loader-2"></i> جارٍ تحديد موقعكم...');

  setTimeout(()=>{
    try{
      if(typeof L === 'undefined') throw new Error('Leaflet library failed to load');
      initMap();
      map.invalidateSize();
    }catch(e){
      console.error('Map init failed:', e);
      const mapEl = document.getElementById('map');
      if(mapEl) mapEl.innerHTML = `<div style="display:flex; align-items:center; justify-content:center; height:100%; text-align:center; color:var(--text-muted); font-size:13px; padding:20px;">تعذر تحميل الخريطة (يرجى التحقق من الاتصال بالإنترنت) — ومع ذلك فإن القائمة والبحث يعملان بشكل طبيعي</div>`;
    }
    try{
      useMyLocation();
    }catch(e){
      console.error('Location init failed:', e);
      setLocationCard('err', '<i class="ti ti-map-pin-off"></i> تعذر تحديد الموقع');
    }
  }, 50);
}

window.addEventListener('load', initApp);