// Password show/hide toggle
function addPasswordToggle() {
  document.querySelectorAll('input[type="password"]').forEach(function(input) {
    if (input.nextElementSibling && input.nextElementSibling.classList.contains('aec-password-toggle')) return;
    var toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'aec-password-toggle';
    toggle.innerHTML = '👁️';
    toggle.style.marginLeft = '8px';
    toggle.onclick = function(e) {
      e.preventDefault();
      input.type = input.type === 'password' ? 'text' : 'password';
      toggle.innerHTML = input.type === 'password' ? '👁️' : '🙈';
    };
    input.parentNode.insertBefore(toggle, input.nextSibling);
  });
}

document.addEventListener('DOMContentLoaded', function() {
  addPasswordToggle();

  // Toast notifications (requires Toastify.js)
  if (window.TempDataMessage) {
    Toastify({
      text: window.TempDataMessage,
      duration: 4000,
      gravity: 'top',
      position: 'right',
      backgroundColor: '#388e3c',
      stopOnFocus: true
    }).showToast();
  }

  // Smooth scroll for anchor links
  document.querySelectorAll('a[href^="#"]').forEach(function(anchor) {
    anchor.addEventListener('click', function(e) {
      var target = document.querySelector(this.getAttribute('href'));
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth' });
      }
    });
  });

  // Responsive navbar auto-close
  document.querySelectorAll('.aec-navbar-links a').forEach(function(link) {
    link.addEventListener('click', function() {
      var navLinks = document.querySelector('.aec-navbar-links');
      if (navLinks && navLinks.classList.contains('active')) {
        navLinks.classList.remove('active');
      }
    });
  });

  // Button ripple effect
  document.querySelectorAll('.aec-btn').forEach(function(btn) {
    btn.addEventListener('click', function(e) {
      var ripple = document.createElement('span');
      ripple.className = 'aec-btn-ripple';
      ripple.style.left = (e.clientX - btn.getBoundingClientRect().left) + 'px';
      ripple.style.top = (e.clientY - btn.getBoundingClientRect().top) + 'px';
      btn.appendChild(ripple);
      setTimeout(function() { ripple.remove(); }, 600);
    });
  });
}); 