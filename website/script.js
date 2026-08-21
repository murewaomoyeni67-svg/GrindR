const views = document.querySelectorAll('[data-panel]');
const navItems = document.querySelectorAll('[data-view]');
const toast = document.querySelector('#toast');
const sidebar = document.querySelector('.sidebar');
const authDialog = document.querySelector('#authDialog');
const authStatus = document.querySelector('#authStatus');
const lifetimePremiumEmails = new Set(['murewaomoyeni67@gmail.com', 'tristynumber1@gmail.com']);
const demoAdmin = { email: 'admin@grindr.local', password: 'GrindrAdmin123!' };
const apiBase = window.location.protocol === 'file:' ? 'http://localhost:5580' : '';
let joinedEmail = localStorage.getItem('grindr-email') || '';

function showToast(message) {
  toast.textContent = message;
  toast.classList.add('show');
  window.clearTimeout(showToast.timeout);
  showToast.timeout = window.setTimeout(() => toast.classList.remove('show'), 2800);
}

function showView(viewName) {
  views.forEach((view) => view.classList.toggle('active-view', view.dataset.panel === viewName));
  navItems.forEach((item) => item.classList.toggle('active', item.dataset.view === viewName));
  sidebar.classList.remove('open');
  if (viewName === 'exercises') loadExercises();
  if (viewName === 'buddies') loadMembers();
}

async function loadMembers() {
  const list = document.querySelector('#buddyList');
  if (!list) return;
  try {
    const response = await fetch(`${apiBase}/api/members`);
    const members = await response.json();
    list.innerHTML = members.length ? members.map((member) => `<article class="buddy"><div class="person-photo">${member.displayName.slice(0, 2).toUpperCase()}</div><div><strong>${member.displayName}</strong><span>Joined member · <i class="online"></i> available</span></div><button aria-label="Connect with ${member.displayName}" data-member="${member.email}">›</button></article>`).join('') : '<div class="empty-state">No members have joined yet. Join discover to become visible to other members.</div>';
    list.querySelectorAll('[data-member]').forEach((button) => button.addEventListener('click', () => showToast('Connection request sent. Chat unlocks after a match.')));
  } catch { list.innerHTML = '<div class="empty-state">Start the backend to load joined members.</div>'; }
}

async function loadExercises(search = '', muscle = '') {
  const grid = document.querySelector('#exerciseGrid');
  if (!grid) return;
  grid.innerHTML = '<p class="loading-state">Loading exercise library...</p>';
  try {
    const response = await fetch(`${apiBase}/api/exercises?page=1&pageSize=24&search=${encodeURIComponent(search)}&muscle=${encodeURIComponent(muscle)}`);
    if (!response.ok) throw new Error('Exercise API unavailable');
    const data = await response.json();
    grid.innerHTML = data.results.map((exercise) => `<article class="exercise-card"><div class="exercise-image" style="background-image:url('${apiBase}${exercise.demoUrl}')"></div><strong>${exercise.name}</strong><span>${exercise.muscle} · ${exercise.equipment} · ${exercise.level}</span></article>`).join('') || '<p class="loading-state">No exercises found.</p>';
  } catch {
    grid.innerHTML = '<p class="loading-state">Start the ASP.NET server to load the exercise library.</p>';
  }
}

navItems.forEach((item) => {
  item.addEventListener('click', () => showView(item.dataset.view));
});

document.querySelector('#menuButton').addEventListener('click', () => sidebar.classList.toggle('open'));
document.querySelector('#accountButton').addEventListener('click', () => authDialog.showModal());
document.querySelector('#closeAuth').addEventListener('click', () => authDialog.close());
document.querySelector('#logoutButton').addEventListener('click', () => showToast('Sign out will connect to your account service.'));
document.querySelector('#checkoutButton').addEventListener('click', () => showToast('Stripe checkout is ready to connect in test mode.'));

function completeSignIn(email) {
  const isLifetimePremium = lifetimePremiumEmails.has(email.toLowerCase());
  authStatus.textContent = isLifetimePremium ? 'Signed in. Lifetime Premium unlocked.' : 'Signed in. Welcome to Grindr.';
  authStatus.className = 'auth-status success';
  window.setTimeout(() => authDialog.close(), 900);
  showToast(isLifetimePremium ? 'Lifetime Premium unlocked.' : 'Welcome back.');
}

document.querySelector('#googleButton').addEventListener('click', () => completeSignIn('google-account'));
document.querySelector('#emailForm').addEventListener('submit', (event) => {
  event.preventDefault();
  const email = document.querySelector('#emailInput').value.trim();
  const password = document.querySelector('#passwordInput').value;
  if (email === demoAdmin.email && password === demoAdmin.password) {
    authStatus.textContent = 'Admin signed in. Full management access enabled.';
    authStatus.className = 'auth-status success';
    window.setTimeout(() => authDialog.close(), 900);
    showToast('Admin access enabled.');
  } else if (password.length >= 8) {
    joinedEmail = email;
    localStorage.setItem('grindr-email', email);
    completeSignIn(email);
  }
});

document.querySelector('#joinDiscoverButton').addEventListener('click', async () => {
  if (!joinedEmail) { authDialog.showModal(); showToast('Sign in first to join discover.'); return; }
  const displayName = window.prompt('What name should other members see?', 'New Member');
  if (!displayName) return;
  await fetch(`${apiBase}/api/members/join`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: joinedEmail, displayName }) });
  await loadMembers();
  showToast('You are now visible to joined members.');
});

document.querySelectorAll('.integration-action').forEach((button) => {
  button.addEventListener('click', () => showToast(`${button.dataset.service} needs a server-side key to connect.`));
});

document.querySelectorAll('.chips button').forEach((chip) => {
  chip.addEventListener('click', () => {
    document.querySelectorAll('.chips button').forEach((item) => item.classList.remove('active'));
    chip.classList.add('active');
    loadExercises(document.querySelector('#exerciseSearch').value, chip.textContent === 'All' ? '' : chip.textContent);
    showToast(`${chip.textContent} exercises selected.`);
  });
});

document.querySelector('#buddySearch').addEventListener('input', (event) => {
  const query = event.target.value.toLowerCase().replace(/[\s-]/g, '');
  document.querySelectorAll('.buddy').forEach((buddy) => {
    const name = buddy.querySelector('strong').textContent.toLowerCase().replace(/[\s-]/g, '');
    buddy.hidden = !name.includes(query);
  });
});

document.querySelector('#exerciseSearch').addEventListener('input', (event) => loadExercises(event.target.value, document.querySelector('.chips button.active')?.textContent === 'All' ? '' : document.querySelector('.chips button.active')?.textContent));

document.querySelectorAll('.buddy button').forEach((button) => {
  button.addEventListener('click', () => showToast('Buddy profile will open here.'));
});

loadExercises();

document.querySelector('#logWorkoutButton')?.addEventListener('click', () => showToast('Workout logged. Your 12 day streak is still alive!'));
document.querySelector('#saveRoutineButton')?.addEventListener('click', () => showToast(`Saved routine: ${document.querySelector('#routineName').value}.`));
document.querySelector('#locationButton')?.addEventListener('click', () => showToast('Location access is ready to connect to OpenStreetMap.'));
document.querySelector('#uploadPhotoButton')?.addEventListener('click', () => showToast('Photo upload is ready to connect to private storage.'));
document.querySelector('#sharePhotoButton')?.addEventListener('click', () => showToast('Watermarked native sharing is ready to connect.'));
document.querySelector('#addMealButton')?.addEventListener('click', () => showToast('Meal logger opened. Add your food and macros here.'));
document.querySelector('#askAiButton')?.addEventListener('click', () => showToast('AI Assistant is ready to connect to Claude.'));