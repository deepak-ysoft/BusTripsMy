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

    // Members Table
    const membersTable = $('#membersTable').DataTable({
        destroy: true,
        processing: true,
        serverSide: false,
        ajax: {
            url: `/Organizations/OrgMembersJson?orgId=${orgId}`,
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
                        </button>`;
                },
                orderable: false,
                searchable: false
            }
        ]
    });

    // Leave As ...

    let orgIdentity, memberType;

    // Open modal and set description
    $(document).on('click', '.leaveBtn', function () {
        orgIdentity = $(this).data('org-id');
        memberType = $(this).data('member-type');

        $('#leaveModalDescription').text(`You're about to leave this organization as ${memberType}. Are you sure?`);
        $('#leaveConfirmModal').modal('show');
    });

    // On confirm, call the leave action via AJAX
    $('#confirmLeaveBtn').click(function () {
        $.ajax({
            url: `/Organizations/SelfRemoveFromOrg?orgId=${orgIdentity}&memberType=${memberType}`,
            type: 'GET',
            success: function (response) {
                $('#leaveConfirmModal').modal('hide');
                showAlert(response.message, response.isSuccess);
                debugger;
                // If member is removed completely, go back to org list
                if (response.isSuccess && response.message === 'Member removed successfully!') {
                    window.location.href = "/Organizations/Index";
                    return;
                }

                // Otherwise, just refresh details dynamically
                if (response.isSuccess) {
                    $.get(`/Organizations/OrgDetailsJson?orgId=${orgIdentity}`, function (res) {
                        if (res.isSuccess) {
                            const newType = res.data.memberType;
                            $('.memberTypeDisplay')
                                .text(`Leave as ${newType}`)
                                .data('member-type', newType); // also update attribute
                        }
                    });
                }
            },
            error: function () {
                showAlert('An error occurred. Please try again.', false);
            }
        });
    });


    // ---------------- Load Overview + Permissions ----------------
    loadOrganizations(orgId);

    function loadOrganizations(orgId) {
        $.get("/Organizations/OrgDetailsJson", { orgId: orgId }, function (data) {
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
                            <input class="form-check-input perm-switch" type="checkbox" data-field="IsView" ${p.isView ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch" type="checkbox" data-field="IsCreate" ${p.isCreate ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch" type="checkbox" data-field="IsEdit" ${p.isEdit ? "checked" : ""} />
                        </div>
                    </td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block">
                            <input class="form-check-input perm-switch" type="checkbox" data-field="IsDeactive" ${p.isDeactive ? "checked" : ""} />
                        </div>
                    </td>
                </tr>`;
            });
            $('#permissionsTable tbody').html(permHtml);
        });
    }

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
            url: `/Organizations/UpdatePermission/${pid}`,
            type: "PUT",
            contentType: "application/json",
            data: JSON.stringify(payload),
            success: res => console.log("Permission updated:", res),
            error: err => console.error("Permission update error:", err)
        });
    });

    // ---------------- Member Details ----------------
    let selectedAction = null;
    let targetUserId = null;
    let organizationId = null;

    $(document).off('click', '.memberDetailsBtn').on('click', '.memberDetailsBtn', function () {
        targetUserId = $(this).data('userid');
        organizationId = $(this).data('orgid');

        $.get('/Organizations/MemberDetails', { userId: targetUserId, orgId: organizationId })
            .done(function (response) {
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

                // Show/hide actions based on member type
                $(".action-btn").show();
                console.log(response.member.memberType);
                if (response.member.memberType === "Creator") $('[data-action="Creator"]').hide();
                if (response.member.memberType === "Admin") $('[data-action="Admin"]').hide();
                if (response.member.memberType === "Member") $('[data-action="Member"]').hide();
                if (response.member.memberType === "ReadOnly") $('[data-action="ReadOnly"]').hide();
            })
            .fail(function (xhr) {
                console.error("Error:", xhr.responseText);
                showAlert("Server error while fetching member details.", false);
            });
    });

    // ---------------- Action Button Click ----------------
    $(document).on("click", ".action-btn", function () {
        const action = $(this).data("action");
        const title = $(this).data("title");
        const message = $(this).data("message");

        switch (action) {
            case "Creator": selectedAction = "Creator"; break;
            case "Admin": selectedAction = "Admin"; break;
            case "Member": selectedAction = "Member"; break;
            case "ReadOnly": selectedAction = "ReadOnly"; break;
        }

        if (!selectedAction) return;

        $("#modalActionTitle").text(title);
        $("#modalActionMessage").text(message);
        $("#confirmActionModal").modal("show");
    });

    // ---------------- Confirm Action ----------------
    $("#confirmModalActionBtn").on("click", function () {
        if (!targetUserId || !organizationId || !selectedAction) return;

        $.post("/Organizations/ChangeOrgMemberRole",
            { targetUserId: targetUserId, orgId: organizationId, action: selectedAction },
            function (res) {
                showAlert(res.message, res.isSuccess);
                if (res.isSuccess) {
                    $("#confirmActionModal").modal("hide");
                    $("#memberDetailsModal").modal("hide");
                    membersTable.ajax.reload(null, false); // refresh members table
                }
            }
        );
    });
});
