$(function () {

    // Initialize DataTable
    var table = $("#organizationsTable").DataTable({
        skipLoader: true,
        ajax: {
            url: '/Admin/GetOrganizationsJson',
            dataSrc: ''
        },
        order: [],
        columns: [
            { data: 'orgName' },
            { data: 'creatorName' },
            { data: 'groupCount' },
            { data: 'isActive', className: "text-center", render: d => d ? '<span class="badge badge-soft-success">Active</span>' : '<span class="badge badge-soft-danger">Not Active</span>' },
            { data: 'isPrimary', className: "text-center", render: d => d ? '<span class="badge badge-soft-success"> Primary</span>' : '<span class="badge badge-soft-danger"> Not Primary</span>' },
            {
                data: 'id',
                className: "text-center",
                render: function (data, type, row) {
                    return `
                    <a href="/Admin/GetOrgDetails?orgId=${data}" class="btn btn-sm btn-soft-primary">
                        <i class="ri-eye-fill"></i>
                    </a>
                    <button data-id="${data}" class="btn btn-sm btn-soft-success editOrgBtn">
                        <i class="ri-edit-2-line"></i>
                    </button>
                    <button class="btn btn-sm btn-soft-danger delete-item"
                        data-url="/Admin/DeleteOrganization?id=${data}" 
                        data-name="${row.orgName}" 
                        data-reload-table="#organizationsTable">
                        <i class="ri-delete-bin-line"></i>
                    </button>`;
                },
                orderable: false,
                searchable: false,
            }
        ]
    });

    $('#organizationsTable_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');

    // Open Edit Organization Modal
    $(document).on("click", ".editOrgBtn", function () {
        const id = $(this).data("id");

        $.get('/Admin/GetOrganization', { id: id })
            .done(function (data) {
                $("#OrganizationId").val(data.id);
                $("#OrgName").val(data.orgName);
                $("#OrgShortName").val(data.shortName);
                $("#OrgDeActiveDiscription").val(data.deActiveDiscription || '');
                $("#OrgIsPrimary").val(data.isPrimary); // form binding
                $("#OrgIsPrimaryDisplay").text(data.isPrimary ? "Yes" : "No"); // display
                $("#UserId").val(data.userId);

                // Initialize switch
                $("#OrgIsActiveToggle").prop("checked", data.isActive);
                $("#OrgIsActive").val(data.isActive); // hidden value
                $("#OrgIsActiveToggleLabel").text(data.isActive ? "Active" : "Inactive");
                $("#deactiveReasonWrapperOrg").toggle(!data.isActive);

                $("#organizationModalLabel").text("Edit Organization");
                $("#organizationModal").modal("show");
            });
    });

    // Toggle change for Organization status
    $(document).on("change", "#OrgIsActiveToggle", function () {
        const isActive = $(this).is(":checked");
        $("#OrgIsActive").val(isActive);
        $("#OrgIsActiveToggleLabel").text(isActive ? "Active" : "Inactive");
        $("#deactiveReasonWrapperOrg").toggle(!isActive);
        if (isActive) $("#OrgDeActiveDiscription").val("");
    });

    // Save (Edit Only) via AJAX
    $("#organizationForm").submit(function (e) {
        e.preventDefault();
        var formData = $(this).serialize();
        $.post('/Admin/AddEditOrg', formData)
            .done(function (res) {
                if (res.isSuccess) {
                    table.ajax.reload();
                    $("#organizationModal").modal("hide");
                    showAlert(res.message, res.isSuccess);
                } else {
                    showAlert(res.message, res.isSuccess);
                }
            });
    });
});