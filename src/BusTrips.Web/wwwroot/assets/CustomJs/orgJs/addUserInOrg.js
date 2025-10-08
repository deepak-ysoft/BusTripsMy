$(document).ready(function () {

    $('#inviteForm').submit(function (e) {
        e.preventDefault();
        var form = $(this);
        var url = form.attr('action');
        var token = $('input[name="__RequestVerificationToken"]', form).val();

        $.ajax({
            type: 'POST',
            url: url,
            data: form.serialize(),
            headers: { 'RequestVerificationToken': token },
            success: function (response) {
                // Clear previous validation messages inside this form only
                form.find('.text-danger').text('');
                form.find('input').removeClass("is-invalid");

                if (response.isSuccess) {
                    $('#inviteMemberModal').modal('hide'); // close modal
                    showAlert(response.message, true);

                    if (response.redirectTo) {
                        window.location.href = response.redirectTo;
                    }
                } else {
                    focusFirstInvalidField(response.errors, "#inviteForm");


                    // Show alert only if no field errors
                    if (response.message) {
                        showAlert(response.message, false);
                    }
                }
            },
            error: function () {
                showAlert("Something went wrong. Please try again.", false);
            }
        });
    });


    $('#addUserForm').submit(function (e) {
        e.preventDefault();
        var form = $(this);
        var url = form.attr('action');
        var token = $('input[name="__RequestVerificationToken"]', form).val();

        $.ajax({
            type: 'POST',
            url: url,
            data: form.serialize(),
            headers: { 'RequestVerificationToken': token },
            success: function (response) {
                form.find('.text-danger').text('');
                form.find('input').removeClass("is-invalid");

                if (response.isSuccess) {
                    if (response.redirectTo) {
                        window.location.href = response.redirectTo;
                        showAlert(response.message, true);
                    } else {
                        showAlert(response.message, true);
                        form[0].reset();
                    }
                } else {
                    focusFirstInvalidField(response.errors, "#addUserForm");

                    if (response.message) {
                        showAlert(response.message, false);
                    }
                }
            },
            error: function () {
                showAlert('Something went wrong. Please try again.', false);
            }
        });
    });

});
