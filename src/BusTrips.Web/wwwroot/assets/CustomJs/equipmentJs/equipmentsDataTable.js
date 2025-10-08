$(document).ready(function () {
    $("#equipmentsTable").DataTable({
        skipLoader: true,
        serverSide: false, // keep false unless you have 100k+ rows
        ajax: {
            url: '/Admin/GetEquipmentsJson',
            dataSrc: ''
        },
        order: [],
        columns: [
            { data: 'busNumber' },
            { data: 'licensePlate' },
            { data: 'issuingProvince' },
            { data: 'manufacturer' },
            { data: 'model' },
            { data: 'year' },
            {
                data: 'isActive',
                className: "text-center",
                render: function (data) {
                    if (data) {
                        return '<span class="badge badge-soft-success"> Active</span>';
                    } else {
                        return '<span class="badge badge-soft-danger">Not Active</span>';
                    }
                }
            },
            {
                orderable: false,
                searchable: false,
                data: 'id',
                className: "text-center",
                render: function (data, type, row) {
                    return `
                                <a href="/Admin/EquipmentDetails?id=${data}" class="btn btn-sm btn-soft-primary"><i class="ri-eye-fill"></i></a>
                                <a href="/Admin/EditEquipment?id=${data}" class="btn btn-sm btn-soft-success"><i class="ri-edit-2-line"></i></a>
                                <button class="btn btn-sm btn-soft-danger delete-item"
                                    data-url="/Admin/DeleteEquipment?id=${data}" 
                                    data-name="${row.busNumber}" 
                                    data-reload-table="#equipmentsTable">
                                    <i class="ri-delete-bin-line"></i>
                                </button>`;
                }
            }
        ]
    }); $('#equipmentsTable_filter input')
        .attr('placeholder', 'Search here...')
        .addClass('form-control');
});