$(document).ready(function () {

    // 1. Toggle IsActive + show/hide inactive fields
    const toggleCheckbox = document.getElementById("IsActiveToggle");
    const hiddenInput = document.getElementById("IsActive");
    const inactiveFields = document.querySelector(".inactive-fields");

    if (toggleCheckbox && hiddenInput) {
        function updateInactiveField() {
            hiddenInput.value = toggleCheckbox.checked; // true/false boolean
            inactiveFields.style.display = toggleCheckbox.checked ? "none" : "block";
        }

        updateInactiveField();
        toggleCheckbox.addEventListener("change", updateInactiveField);
    }

    // 2. Dynamic Documents
    const container = document.getElementById("documentsContainer");
    const addBtn = document.getElementById("addDocumentBtn");
    let docIndex = 0;

    addBtn.addEventListener("click", function () {
        const docItem = document.createElement("div");
        docItem.classList.add("row", "g-2", "mb-2");

        docItem.innerHTML = `
            <div class="col-md-6">
                <input type="file" name="Documents[${docIndex}].File" class="form-control" required />
            </div>
            <div class="col-md-5">
                <input type="text" name="Documents[${docIndex}].Description" class="form-control" placeholder="Description" required />
            </div>
            <div class="col-md-1">
                <button type="button" class="btn btn-danger btn-sm removeDocBtn mt-1">&times;</button>
            </div>
        `;

        container.appendChild(docItem);

        docItem.querySelector(".removeDocBtn").addEventListener("click", function () {
            container.removeChild(docItem);
        });

        docIndex++;
    });

    // 3. Year max validation
    $("#Year").on("input", function () {
        const currentYear = new Date().getFullYear();
        if (parseInt(this.value) > currentYear) this.value = currentYear;
    });

    // 4. AJAX form submit
    $("#equipmentForm").submit(function (e) {
        e.preventDefault();
        const form = $(this)[0];
        const formData = new FormData(form);

        $.ajax({
            url: form.action,
            type: form.method,
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                $(".text-danger").text(""); // clear old errors
                $("#validationSummary").empty();

                if (res.isSuccess) {
                    showAlert(res.message, true);
                    window.location.href = "/Admin/Equipments";
                }
                else {
                    if (res.message) {
                        showAlert(res.message, false);
                    } else {
                        focusFirstInvalidField(res.errors, "#equipmentForm");
                    }
                }
            },
            error: function () {
                showAlert("An error occurred while submitting the form.", false);
            }
        });
    });
});
