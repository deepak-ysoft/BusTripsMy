/*
Template Name: Chartered_Bus 
Author: Themesbrand
Version: 2.2.0
Website: https://Themesbrand.com/
Contact: Themesbrand@gmail.com
File: Layout Js File
*/

// Function to handle delete
function handleDeleteClick(element) {
    const deleteUrl = element.getAttribute("data-url");
    const deleteName = element.getAttribute("data-name") || "this item";
    const reloadTableSelector = element.getAttribute("data-reload-table") || "";

    document.getElementById("deleteModalBody").innerText = `Are you sure you want to delete "${deleteName}"?`;

    const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
    modal.show();

    // Confirm delete button
    const confirmBtn = document.getElementById("confirmDeleteBtn");
    confirmBtn.onclick = function () {
        fetch(deleteUrl, { method: "DELETE" })
            .then(response => response.json())
            .then(res => {
                if (res.isSuccess) {
                    showAlert(res.message, true);

                    // reload table if using DataTables
                    if (reloadTableSelector && $.fn.DataTable.isDataTable(reloadTableSelector)) {
                        $(reloadTableSelector).DataTable().ajax.reload(null, false);
                    }

                    // hide modal
                    modal.hide();
                } else {
                    showAlert(res.message, false);
                }
            })
            .catch(() => {
                showAlert("Error deleting " + deleteName, false);
            });
    }
}

// Attach click only when an element exists
document.addEventListener("click", function (e) {
    if (e.target.closest(".delete-item")) {
        handleDeleteClick(e.target.closest(".delete-item"));
    }
});

function showAlert(message, isSuccess = true, duration = 5000) {
    // Create a unique ID for this alert
    const alertId = `shared-alert-${Date.now()}`;
    const alertHtml = `
        <div id="${alertId}" class="alert ${isSuccess ? "alert-success" : "alert-danger"} 
             alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow mx-5 px-5" 
             role="alert" style="z-index:10000 !important;">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>`;

    // Append to body
    $("body").append(alertHtml);

    const alertEl = document.getElementById(alertId);
    if (!alertEl) return;

    // Create Bootstrap alert instance
    const bsAlert = bootstrap.Alert.getOrCreateInstance(alertEl);

    // Auto-hide after duration
    setTimeout(() => {
        if (alertEl && bsAlert) {
            // Remove the alert safely
            bsAlert.close();
            // Wait for fade transition to finish before disposing
            alertEl.addEventListener('transitionend', () => {
                bsAlert.dispose();
                if (alertEl.parentNode) {
                    alertEl.parentNode.removeChild(alertEl);
                }
            });
        }
    }, duration);
}

function focusFirstInvalidField(errors, formSelector = "") {
    const $form = formSelector ? $(formSelector) : $(document);
    let firstInvalidField = null;

    // Clear previous errors
    $form.find("input, textarea, select").removeClass("is-invalid");
    $form.find("span.text-danger").text("");

    // Iterate form fields in visual order
    $form.find("input, textarea, select").each(function () {
        const name = $(this).attr("name");
        if (errors[name]) {
            const messages = Array.isArray(errors[name]) ? errors[name] : [errors[name]];

            // Show error message
            const $errorSpan = $form.find(`span[data-valmsg-for='${name}']`);
            if ($errorSpan.length) $errorSpan.text(messages[0]);

            // Focus first invalid field
            if (!firstInvalidField) firstInvalidField = $(this);
        }
    });

    if (firstInvalidField && firstInvalidField.length) {
        firstInvalidField[0].scrollIntoView({ behavior: "smooth", block: "center" });
        firstInvalidField.addClass("is-invalid");
        firstInvalidField.focus();
    }
}

// Clear errors on input/change
$(document).on("input change", "input, textarea, select", function () {
    const name = $(this).attr("name");
    $(this).removeClass("is-invalid");
    $(`[data-valmsg-for='${name}']`).text('');
});

(function () {

    'use strict';

    if (sessionStorage.getItem('defaultAttribute')) {

        var attributesValue = document.documentElement.attributes;
        var CurrentLayoutAttributes = {};
        Object.entries(attributesValue).forEach(function(key) {
            if (key[1] && key[1].nodeName && key[1].nodeName != "undefined") {
                var nodeKey = key[1].nodeName;
                CurrentLayoutAttributes[nodeKey] = key[1].nodeValue;
            }
          });
        if(sessionStorage.getItem('defaultAttribute') !== JSON.stringify(CurrentLayoutAttributes)) {
            sessionStorage.clear();
            window.location.reload();
        } else {
            var isLayoutAttributes = {};
            isLayoutAttributes['data-layout'] = sessionStorage.getItem('data-layout');
            isLayoutAttributes['data-sidebar-size'] = sessionStorage.getItem('data-sidebar-size');
            isLayoutAttributes['data-layout-mode'] = sessionStorage.getItem('data-layout-mode');
            isLayoutAttributes['data-layout-width'] = sessionStorage.getItem('data-layout-width');
            isLayoutAttributes['data-sidebar'] = sessionStorage.getItem('data-sidebar');
            isLayoutAttributes['data-sidebar-image'] = sessionStorage.getItem('data-sidebar-image');
            isLayoutAttributes['data-layout-direction'] = sessionStorage.getItem('data-layout-direction');
            isLayoutAttributes['data-layout-position'] = sessionStorage.getItem('data-layout-position');
            isLayoutAttributes['data-layout-style'] = sessionStorage.getItem('data-layout-style');
            isLayoutAttributes['data-topbar'] = sessionStorage.getItem('data-topbar');
            isLayoutAttributes['data-preloader'] = sessionStorage.getItem('data-preloader');
            isLayoutAttributes['data-body-image'] = sessionStorage.getItem('data-body-image');
            
            Object.keys(isLayoutAttributes).forEach(function (x) {
                if (isLayoutAttributes[x] && isLayoutAttributes[x]) {
                    document.documentElement.setAttribute(x, isLayoutAttributes[x]);
                }
            });
        }
    }

})();