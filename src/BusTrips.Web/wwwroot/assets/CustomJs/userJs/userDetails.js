$(function () {    // Handle tab activation from query string
    const urlParams = new URLSearchParams(window.location.search);
    const activeTab = urlParams.get("tab");
    if (activeTab) {
        const tabTrigger = document.querySelector(`[data-bs-toggle="tab"][href="#${activeTab}"]`);
        if (tabTrigger) new bootstrap.Tab(tabTrigger).show();
    }

    // Update URL when tab is clicked/changed
    $('a[data-bs-toggle="tab"]').on("shown.bs.tab", function (e) {
        const tabId = $(e.target).attr("href").substring(1);
        const newUrl = new URL(window.location);
        newUrl.searchParams.set("tab", tabId);
        window.history.replaceState(null, "", newUrl);
    });

    // Show/hide inactive reason
    $(document).on("change", "#IsActive", function () {
        $("#deactiveReasonWrapper").toggle($(this).val() === "false");
        if ($(this).val() !== "false") $("#DeActiveDiscription").val("");
    });
    $(document).on("change", "#OrgIsActive", function () {
        $("#deactiveReasonWrapperOrg").toggle($(this).val() === "false");
        if ($(this).val() !== "false") $("#OrgDeActiveDiscription").val("");
    });

    // Load organizations dynamically
    function loadOrganizations(expandOrgId = null) {
        $.get("/Admin/GetUserOrganizations", { userId: "@Model.UserId" }, function (data) {
            $("#orgContainer").html(data);

            // If we have an orgId to expand, open its collapse
            if (expandOrgId) {
                $("#orgBody_" + expandOrgId).collapse("show");
            }
        });
    }


    // Clear field-specific validation on input/change
    $("#organizationForm").on("input", "input", function () {
        var fieldName = $(this).attr("name");
        $('[data-valmsg-for="' + fieldName + '"]').text('');
    });


    // On submit for Organization
    $("#organizationForm").on("submit", function (e) {
        e.preventDefault();
        let orgId = $("#OrganizationId").val();
        $.post("/Admin/AddEditOrg", $(this).serialize(), function (response) {
            if (response.isSuccess) {
                loadOrganizations(orgId); // expand this org
                showAlert(response.message, response.isSuccess)
                $("#organizationModal").modal("hide");
            } else
                showAlert(response.message, response.isSuccess)
        });
    });

    // Open Organization modal
    $(document).on("click", ".createOrgBtn, .editOrgBtn", function () {
        let modal = $("#organizationModal");
        let isEdit = $(this).hasClass("editOrgBtn");
        modal.find("form")[0].reset();
        modal.find(".show-Org-fields").toggle(isEdit);
        $("#deactiveReasonWrapperOrg").hide();

        if (isEdit) {
            let orgId = $(this).data("id");
            $.get("/Admin/GetOrganization", { id: orgId }, function (data) {
                $("#organizationModalLabel").text("Edit Organization");
                $("#OrganizationId").val(data.id);
                $("#OrgName").val(data.orgName);
                $("#OrgShortName").val(data.shortName);
                $("#OrgIsActive").val(data.isActive.toString()).trigger("change");
                $("#OrgDeActiveDiscription").val(data.deActiveDiscription);
                modal.find(".show-Org-fields").show();
                modal.modal("show");
            });
        } else {
            $("#organizationModalLabel").text("Create Organization");
            modal.modal("show");
        }
    });
});