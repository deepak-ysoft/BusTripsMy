function initDataTables() {
    // Organization Table
    if ($('#Organization-List').length) {
        $('#Organization-List').DataTable({
            paging: true,
            searching: true,
            ordering: true,
            responsive: true,
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50, 100],
            destroy: true, // important for re-init
            language: {
                search: "Search :",
                lengthMenu: "Show records  _MENU_",
                zeroRecords: "No matching records found",
                info: "Showing _START_ to _END_ of _TOTAL_ records",
                infoEmpty: "No records available",
                infoFiltered: "(filtered from _MAX_ total records)"
            }
        });
    }

    // Trip Table
    if ($('#Trip-List').length) {
        $('#Trip-List').DataTable({
            paging: true,
            searching: true,
            ordering: true,
            order: [],      // 👈 prevents default ordering on first column
            responsive: true,
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50, 100],
            destroy: true,
            language: {
                search: "Search :",
                lengthMenu: "Show records  _MENU_",
                zeroRecords: "No matching records found",
                info: "Showing _START_ to _END_ of _TOTAL_ records",
                infoEmpty: "No records available",
                infoFiltered: "(filtered from _MAX_ total records)"
            }
        });
    }

    // Group Tables
    $('.groupTable').each(function () {
        $(this).DataTable({
            paging: true,
            searching: true,
            ordering: true,
            responsive: true,
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50, 100],
            destroy: true,
            language: {
                search: "Search :",
                lengthMenu: "Show records _MENU_",
                zeroRecords: "No matching records found",
                info: "Showing _START_ to _END_ of _TOTAL_ records",
                infoEmpty: "No records available",
                infoFiltered: "(filtered from _MAX_ total records)"
            }
        });
    });
}

// Run once on page load
$(document).ready(function () {
    initDataTables();
});
