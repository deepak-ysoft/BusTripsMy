$(document).ready(function () {
    let table = $("#trip-table").DataTable({
        order: [],
        serverSide: false,
        ajax: {
            url: '/Admin/GetTripsJson',
            data: {
                groupId: groupId,
                userId: safeUserId,
                bucket: '@Context.Request.Query["bucket"]'
            },
            dataSrc: ''
        },
        columns: [
            { data: "tripName" },
            { data: "departureDate" },
            { data: "departureTime" },
            { data: "destinationArrivalDate" },
            { data: "destinationArrivalTime" },
            {
                data: "status", className: "text-center", render: function (d, type, row) {
                    switch (d) {
                        case 'Draft':
                            return '<span class="badge badge-soft-secondary">Draft</span>';
                        case 'Quoted':
                            return '<span class="badge badge-soft-success">Quoted</span>';
                        case 'Approved':
                            return '<span class="badge badge-soft-success">Approved</span>';
                        case 'Rejected':
                            return '<span class="badge badge-soft-danger">Rejected</span>';
                        case 'Live':
                            return '<span class="badge badge-soft-primary">Live</span>';
                        case 'Completed':
                            return '<span class="badge badge-soft-primary">Completed</span>';
                        case 'Canceled':
                            return '<span class="badge badge-soft-warning">Canceled</span>';
                        default:
                            return '<span class="badge badge-soft-secondary">' + d + '</span>';
                    }
                }
            },
            {
                data: "id",
                className: "text-center",
                render: function (id, type, row) {
                    const editUrl = `/Admin/EditTrip?id=${id}&userId=${safeUserId}&returnUrl=${encodeURIComponent(returnUrl)}&controller=Admin`;
                    return `
                            <div class="text-center">
                                <a href="/Admin/TripDetails?tripId=${id}&reqFrom=groupDetails" class="btn btn-sm btn-soft-primary me-1"><i class="ri-eye-fill"></i></a>
                                <a href="${editUrl}" class="btn btn-sm btn-soft-success me-1" title="Edit">
                                    <i class="ri-edit-2-line"></i>
                                </a>
                                            
                                <button class="btn btn-sm btn-soft-danger delete-item"
                                    data-url="/Admin/DeleteTrip?id=${id}" 
                                    data-name="${row.tripName}" 
                                    data-reload-table="#trip-table">
                                    <i class="ri-delete-bin-line"></i>
                                </button>
                            </div>
                        `;
                },
                orderable: false,
                searchable: false
            }
        ]
    });
    $('#trip-table_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');
});