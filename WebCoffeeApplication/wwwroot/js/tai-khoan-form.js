document.addEventListener('DOMContentLoaded', function () {
  // Chỉ xử lý trên các form tài khoản có gắn data attribute.
  var form = document.querySelector('form[data-tai-khoan-form="true"]');
  if (!form) {
    return;
  }

  // Hiển thị toast cảnh báo với 1 hoặc nhiều thông điệp lỗi.
  function showWarningToast(messages) {
    var toastBody = document.getElementById('toastMessage');
    if (!toastBody) {
      return;
    }

    if (Array.isArray(messages)) {
      toastBody.innerHTML = '';
      var ul = document.createElement('ul');
      ul.className = 'mb-0 ps-3';
      messages.forEach(function (msg) {
        var li = document.createElement('li');
        li.textContent = msg;
        ul.appendChild(li);
      });
      toastBody.appendChild(ul);
    } else {
      toastBody.textContent = messages;
    }

    var toastEl = document.getElementById('warningToast');
    if (toastEl) {
      var toast = new bootstrap.Toast(toastEl);
      toast.show();
    }
  }

  form.addEventListener('submit', function (e) {
    // Thu thập danh sách lỗi trước khi cho phép submit.
    var errors = [];
    // Create yêu cầu mật khẩu, Edit thì không.
    var requirePassword = form.getAttribute('data-require-password') === 'true';

    // Kiểm tra họ tên.
    var hoTen = document.getElementById('HoTen');
    if (!hoTen || !hoTen.value || hoTen.value.trim() === '') {
      errors.push('Vui lòng nhập họ và tên.');
    } else if (hoTen.value.trim().length > 100) {
      errors.push('Họ và tên không được vượt quá 100 ký tự.');
    }

    // Kiểm tra vai trò.
    var vaiTro = document.getElementById('VaiTro');
    if (!vaiTro || !vaiTro.value || (vaiTro.value !== '1' && vaiTro.value !== '2')) {
      errors.push('Vui lòng chọn vai trò hợp lệ.');
    }

    // Kiểm tra số điện thoại.
    var sdt = document.getElementById('SDT');
    if (!sdt || !sdt.value || sdt.value.trim() === '') {
      errors.push('Vui lòng nhập số điện thoại.');
    } else if (!/^\d{10,11}$/.test(sdt.value.trim())) {
      errors.push('Số điện thoại phải gồm 10-11 chữ số.');
    }

    // Kiểm tra tên đăng nhập.
    var userName = document.getElementById('UserName');
    if (!userName || !userName.value || userName.value.trim() === '') {
      errors.push('Vui lòng nhập tên đăng nhập.');
    }

    // Kiểm tra email bắt buộc và đúng định dạng.
    var email = document.getElementById('Email');
    if (!email || !email.value || email.value.trim() === '') {
      errors.push('Vui lòng nhập email.');
    } else {
      var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(email.value.trim())) {
        errors.push('Email không đúng định dạng.');
      }
    }

    if (requirePassword) {
      // Kiểm tra mật khẩu và xác nhận mật khẩu ở màn hình Create.
      var password = document.getElementById('Password');
      var confirmPassword = document.getElementById('ConfirmPassword');

      if (!password || !password.value) {
        errors.push('Vui lòng nhập mật khẩu.');
      } else if (password.value.length < 6) {
        errors.push('Mật khẩu phải có ít nhất 6 ký tự.');
      }

      if (!confirmPassword || !confirmPassword.value) {
        errors.push('Vui lòng nhập xác nhận mật khẩu.');
      } else if (password && confirmPassword.value !== password.value) {
        errors.push('Mật khẩu xác nhận không khớp.');
      }
    }

    // Chặn submit và hiển thị toast nếu có lỗi.
    if (errors.length > 0) {
      e.preventDefault();
      showWarningToast(errors);
    }
  });
});
