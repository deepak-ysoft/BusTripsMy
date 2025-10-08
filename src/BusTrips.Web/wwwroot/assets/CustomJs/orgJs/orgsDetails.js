$(document).ready(function () {

    // Handle tab activation from query string
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

    // Org Id
    const orgId = $('#orgDetailsWrapper').data('org-id');
    

    // ---------------- Init DataTables ----------------


    // Groups Table
    const groupsTable = $('#groupsTable').DataTable({
        destroy: true,
        skipLoader: true,
        serverSide: false,
        ajax: {
            url: `/Admin/OrgGroupsJson?orgId=${orgId}`,
            dataSrc: ''
        },
        order: [],
        columns: [
            { data: 'groupName' },
            { data: 'shortName' },
            { data: 'description' },
            { data: 'tripsCount' },
            { data: 'status', className: "text-center" },
            {
                orderable: false,
                searchable: false,
                data: 'id',
                className: "text-center",
                render: function (id, type, row) {
                    return `
                        <a href="/Admin/GroupDetails?groupId=${id}&userId=${row.userId}" class="btn btn-sm btn-soft-primary">
                            <i class="ri-eye-fill"></i>
                        </a>
                        <a class="btn btn-sm btn-soft-success editGroupBtn" data-id="${id}">
                            <i class="ri-edit-2-line"></i>
                        </a>
                        <button class="btn btn-sm btn-soft-danger delete-item"
                            data-url="/Admin/DeleteGroup?id=${id}" 
                            data-name="${row.groupName}" 
                            data-reload-table="#groupsTable">
                            <i class="ri-delete-bin-line"></i>
                        </button>`;
                }
            }
        ]
    });

    $('#groupsTable_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');
    // Members Table
    const membersTable = $('#membersTable').DataTable({
        destroy: true,
        processing: true,
        serverSide: false,
        skipLoader: true,
        ajax: {
            url: `/Admin/OrgMembersJson?orgId=${orgId}`,
            dataSrc: ''
        },
        order: [],
        columns: [
            { data: 'userName' },
            { data: 'email' },
            { data: 'phoneNumber' },
            {
                data: 'memberType',
                className: 'text-center',
                render: function (d) {
                    switch (d) {
                        case 'Creator': return '<span class="badge badge-soft-primary">Creator</span>';
                        case 'Admin': return '<span class="badge badge-soft-success">Admin</span>';
                        case 'Member': return '<span class="badge badge-soft-warning">Member</span>';
                        case 'ReadOnly':
                        case 'Read Only': return '<span class="badge badge-soft-danger">Read Only</span>';
                        default: return '<span class="badge badge-soft-secondary">' + d + '</span>';
                    }
                }
            },
            {
                data: 'id',
                className: 'text-center',
                render: function (id, type, row) {
                    return `
                        <button class="btn btn-sm btn-soft-primary memberDetailsBtn" data-userid="${row.appUserId}" data-orgid="${row.organizationId}" title="View Details">
                            <i class="ri-eye-fill"></i>
                        </button>
                        <button class="btn btn-sm btn-soft-danger delete-item"
                            data-url="/Admin/DeleteOrgMember?id=${id}" 
                            data-name="${row.userName}" 
                            data-reload-table="#membersTable">
                            <i class="ri-delete-bin-line"></i>
                        </button>`;
                },
                orderable: false,
                searchable: false
            }
        ]
    });

    $('#membersTable_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');

    // ---------------- Load Overview + Permissions ----------------
    loadOrganizations(orgId);

    function loadOrganizations(orgId) {
        $.get("/Admin/OrgDetailsJson", { orgId: orgId }, function (data) {
            if (!data.isSuccess) {
                showAlert(data.message, false);
                return;
            }

            const org = data.data;

            // Overview
            $('#orgName').text(org.orgName || '');
            $('#orgShortName').text(org.shortName || '');
            $('#orgStatus').text(org.isActive ? "Active" : "Inactive");
            $('#orgPrimary').text(org.isPrimary ? "Yes" : "No");
            $('#creatorName').text(org.creator?.fullName || '');
            $('#creatorEmail').text(org.creator?.email || '');
            $('#creatorPhone').text(org.creator?.number || '');

            // Permissions
            let permHtml = '';
            (org.permissions || []).forEach(p => {
                permHtml += `
                <tr data-id="${p.pId}">
                    <td>${p.memberType}</td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch custom-caret" type="checkbox" data-field="IsView" ${p.isView ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch custom-caret" type="checkbox" data-field="IsCreate" ${p.isCreate ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch custom-caret" type="checkbox" data-field="IsEdit" ${p.isEdit ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch custom-caret" type="checkbox" data-field="IsDeactive" ${p.isDeactive ? "checked" : ""} />
                        </div>
                    </td>
                </tr>`;
            });
            $('#permissionsTable tbody').html(permHtml);
        });
    }

    // ---------------- Group Modal ----------------
    $(document).on("click", ".createGroupBtn, .editGroupBtn", function () {
        const modal = $("#groupModal");
        const isEdit = $(this).hasClass("editGroupBtn");

        // Reset form and hide deactivation reason
        modal.find("form")[0].reset();
        $("#deactiveReasonWrapper").hide();

        if (isEdit) {
            modal.find(".show-fields").show();

            const groupId = $(this).data("id");
            $.get("/Admin/GetGroup", { id: groupId }, function (data) {
                $("#groupModalLabel").text("Edit Group");
                $("#Id").val(data.id);
                $("#OrgId").val(data.orgId);
                $("#GroupName").val(data.groupName);
                $("#ShortName").val(data.shortName);
                $("#Description").val(data.description);

                // Set toggle and label based on isActive
                $("#IsActiveToggle").prop("checked", data.isActive);
                $("#IsActiveToggle").next("label").text(data.isActive ? "Active" : "Inactive");
                $("#IsActive").val(data.isActive);

                // Show reason if inactive
                $("#DeActiveDiscription").val(data.deActiveDiscription);
                $("#deactiveReasonWrapper").toggle(!data.isActive);

                modal.modal("show");
            });
        } else {
            modal.find(".show-fields").hide();
            $("#groupModalLabel").text("Create Group");
            $("#OrgId").val($(this).data("orgid"));

            // Default toggle to active
            $("#IsActiveToggle").prop("checked", true);
            $("#IsActiveToggle").next("label").text("Active");
            $("#IsActive").val(true);

            modal.modal("show");
        }
    });

    // Toggle change event
    $("#IsActiveToggle").on("change", function () {
        const isActive = $(this).is(":checked");

        // Update hidden field
        $("#IsActive").val(isActive);

        // Update label text
        $(this).next("label").text(isActive ? "Active" : "Inactive");

        // Show/hide reason field and clear value if active
        if (isActive) {
            $("#deactiveReasonWrapper").hide();
            $("#DeActiveDiscription").val(""); // clear textarea
        } else {
            $("#deactiveReasonWrapper").show();
        }
    });

    $("#groupForm").on("submit", function (e) {
        e.preventDefault();
        $.post("/Admin/AddEditGroup", $(this).serialize(), function (response) {
            showAlert(response.message, response.isSuccess);
            if (response.isSuccess) {
                $("#groupModal").modal("hide");
                groupsTable.ajax.reload(null, false); // refresh only groups
            }
        });
    });

    // ---------------- Permissions ----------------
    $('#permissionsTable').on('change', '.perm-switch', function () {
        const row = $(this).closest('tr');
        const pid = row.data('id');
        const field = $(this).data('field');
        const value = $(this).is(':checked');

        const payload = {
            orgId: orgId,
            memberType: row.find('td:first').text().trim(),
            isView: field === "IsView" ? value : null,
            isCreate: field === "IsCreate" ? value : null,
            isEdit: field === "IsEdit" ? value : null,
            isDeactive: field === "IsDeactive" ? value : null
        };

        $.ajax({
            url: `/Admin/UpdatePermission/${pid}`,
            type: "PUT",
            skipLoader: true,
            contentType: "application/json",
            data: JSON.stringify(payload),
            success: res => console.log("Permission updated:", res),
            error: err => console.error("Permission update error:", err)
        });
    });

    // ---------------- Member Details ----------------
    let selectedRole = null;
    let memberShipId = null;

    $(document).on('click', '.memberDetailsBtn', function () {
        var userId = $(this).data('userid');
        var orgId = $(this).data('orgid');

        $.get('/Admin/MemberDetails', { userId: userId, orgId: orgId }, function (response) {
            if (!response) {
                showAlert("Member details not found.", false);
                return;
            }

            $('#memberDetailsModal').modal('show');
            $('#detailUserName').text(response.member.userName);
            $('#detailEmail').text(response.member.email);
            $('#detailPhone').text(response.member.phoneNumber);
            $('#detailOrgName').text(response.org.orgName);
            $('#detailOrgStatus').removeClass().addClass('badge ' + (response.org.isActive ? 'bg-success' : 'bg-warning'))
                .text(response.org.isActive ? 'Active' : 'Inactive');

            let badgeClass = "bg-secondary";
            switch (response.member.memberType) {
                case "Creator": badgeClass = "bg-primary"; break;
                case "Admin": badgeClass = "bg-success"; break;
                case "Member": badgeClass = "bg-warning"; break;
                case "ReadOnly": badgeClass = "bg-danger"; break;
            }
            $('#detailMemberType').removeClass().addClass('badge ' + badgeClass).text(response.member.memberType);

            memberShipId = response.member.id;
            $("#memberDetailsModal").data("memberId", memberShipId);

            $(".action-btn").show();
            if (response.member.memberType === "Creator") $('[data-action="makeCreator"]').hide();
            if (response.member.memberType === "Admin") $('[data-action="toggleAdmin"]').hide();
            if (response.member.memberType === "Member") $('[data-action="toggleMember"]').hide();
            if (response.member.memberType === "ReadOnly") $('[data-action="makeReadonly"]').hide();
        });
    });

    $(document).on("click", ".action-btn", function () {
        const action = $(this).data("action");
        const title = $(this).data("title");
        const message = $(this).data("message");

        switch (action) {
            case "makeCreator": selectedRole = "Creator"; break;
            case "toggleAdmin": selectedRole = "Admin"; break;
            case "toggleMember": selectedRole = "Member"; break;
            case "makeReadonly": selectedRole = "ReadOnly"; break;
        }

        $("#modalActionTitle").text(title);
        $("#modalActionMessage").text(message);
        $("#confirmActionModal").modal("show");
    });

    $("#confirmModalActionBtn").on("click", function () {
        if (!memberShipId || !selectedRole) return;

        $.post("/Admin/ChangeMemberRole", { memberShipId: memberShipId, newRole: selectedRole }, function (res) {
            showAlert(res.message, res.isSuccess);
            if (res.isSuccess) {
                $("#confirmActionModal").modal("hide");
                $("#memberDetailsModal").modal("hide");
                membersTable.ajax.reload(null, false); // refresh members table
            }
        });
    });
});
