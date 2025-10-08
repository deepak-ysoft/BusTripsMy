let table;

$(document).ready(function () {
    // Initialize DataTable
    table = $("#usersTable").DataTable({
        ajax: {
            url: '/Admin/GetUsersJson',
            dataSrc: '',
            orderable: false
        },
        order: [],
        columns: [
            {
                data: 'photoUrl',
                render: function (data) {
                    const fallbackImg = 'https://img.freepik.com/premium-psd/contact-icon-illustration-isolated_23-2151903357.jpg?semt=ais_hybrid&w=740&q=80';
                    const imgSrc = data ? data : fallbackImg;
                    return `
                            <img src="${imgSrc}" 
                                 alt="user-img" 
                                 class="img-thumbnail rounded-circle" 
                                 style="width:30px; height:30px;" 
                                 onerror="this.onerror=null; this.src='${fallbackImg}';" />`;
                },
                orderable: false,
                searchable: false,
                className: "text-center"
            },
            {
                data: null,
                render: d => `${d.firstName} ${d.lastName}`
            },
            { data: 'email' },
            { data: 'phoneNumber' },
            {
                data: 'emailConfirmed',
                className: "text-center",
                render: d => d ? '<span class="badge badge-soft-success">Confirmed</span>' : '<span class="badge badge-soft-danger">Not Confirmed</span>'
            },
            {
                data: 'isActive',
                className: "text-center",
                render: d => d ? '<span class="badge badge-soft-success">Active</span>' : '<span class="badge badge-soft-danger">Not Active</span>'
            },
            {
                data: 'userId',
                render: function (data, type, row) {
                    return `
                <button data-user-id="${data}" class="btn btn-sm btn-soft-primary user-details">
                    <i class="ri-eye-fill"></i>
                </button>
                <button data-user-id="${data}" class="btn btn-sm btn-soft-success edit-user">
                    <i class="ri-edit-2-line"></i>
                </button>
                <button class="btn btn-sm btn-soft-danger delete-item" 
                        data-url="/Admin/DeleteUser?userId=${data}" 
                        data-name="${row.firstName} ${row.lastName}" 
                        data-reload-table="#usersTable">
                    <i class="ri-delete-bin-line"></i>
                </button>
            `;
                },
                orderable: false,
                searchable: false,
                className: "text-center"
            }
        ]

    });

    $('#usersTable_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');

    // Edit redirect
    $(document).on("click", ".edit-user", function () {
        const userId = $(this).data("user-id");
        window.location.href = `/Admin/EditUser?userId=${userId}`;
    });

    // details redirect
    $(document).on("click", ".user-details", function () {
        const userId = $(this).data("user-id");
        window.location.href = `/Admin/UserDetails?userId=${userId}`;
    });
});