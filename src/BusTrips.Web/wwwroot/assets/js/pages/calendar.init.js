document.addEventListener("DOMContentLoaded", function () {
    var calendarEl = document.getElementById('calendar');
    var defaultEvents = [];

    async function loadEvents() {
        try {
            const response = await fetch("/Home/GetAllTrips");
            if (!response.ok) throw new Error("Failed to fetch events");

            const data = await response.json();

            // Map all trips for calendar display
            defaultEvents = data.all.map((t) => ({
                id: t.id,
                title: t.tripName,
                start: t.departureDate ? new Date(t.departureDate) : null,
                end: t.destinationArrivalDate ? new Date(t.destinationArrivalDate) : null,
                className: getClassByStatus(t.status),
                allDay: true,
                extendedProps: {
                    tripId: t.id,
                    groupId: t.groupId,
                    groupName: t.groupName,
                    organizationName: t.organizationName,
                    organizationCreatorName: t.organizationCreatorName,
                    status: t.status,
                    departureDate: t.departureDate,
                    departureTime: t.departureTime,
                    startLocation: t.startLocation,
                    destinationArrivalDate: t.destinationArrivalDate,
                    destinationArrivalTime: t.destinationArrivalTime,
                    destinationLocation: t.destinationLocation,

                    driverId: t?.driver?.driverId,
                    driverName: t?.driver?.driverName,
                    employmentType: t?.driver?.employmentType,
                    licenseNumber: t?.driver?.licenseNumber,
                    licenseProvince: t?.driver?.licenseProvince,

                    manufacturer: t?.equipment?.manufacturer,
                    model: t?.equipment?.model,
                    licensePlate: t?.equipment?.licensePlate,
                    seatingCapacity: t?.equipment?.seatingCapacity,
                    year: t?.equipment?.year,

                    passengers: t.passengers,
                    tripDays: t.tripDays,
                    notes: t.notes
                }
            }));

            upcoming = data.upcoming.map((t) => ({
                id: t.id,
                title: t.tripName,
                start: t.departureDate ? new Date(t.departureDate) : null,
                end: t.destinationArrivalDate ? new Date(t.destinationArrivalDate) : null,
                className: getClassByStatus(t.status),
                allDay: true,
                extendedProps: {
                    tripId: t.id,
                    groupId: t.groupId,
                    groupName: t.groupName,
                    organizationName: t.organizationName,
                    organizationCreatorName: t.organizationCreatorName,
                    status: t.status,
                    departureDate: t.departureDate,
                    departureTime: t.departureTime,
                    startLocation: t.startLocation,
                    destinationArrivalDate: t.destinationArrivalDate,
                    destinationArrivalTime: t.destinationArrivalTime,
                    destinationLocation: t.destinationLocation,

                    driverId: t?.driver?.driverId,
                    driverName: t?.driver?.driverName,
                    employmentType: t?.driver?.employmentType,
                    licenseNumber: t?.driver?.licenseNumber,
                    licenseProvince: t?.driver?.licenseProvince,

                    manufacturer: t?.equipment?.manufacturer,
                    model: t?.equipment?.model,
                    licensePlate: t?.equipment?.licensePlate,
                    seatingCapacity: t?.equipment?.seatingCapacity,
                    year: t?.equipment?.year,

                    passengers: t.passengers,
                    tripDays: t.tripDays,
                    notes: t.notes
                }
            }));

            calendar.removeAllEvents();
            calendar.addEventSource(defaultEvents);
            renderUpcomingEvents(upcoming);
        } catch (error) {
            console.error("Error loading events:", error);
        }
    }

    function getClassByStatus(status) {
        switch (status) {
            case "Draft": return "bg-soft-secondary";
            case "Quoted": return "bg-soft-info";
            case "Approved": return "bg-soft-success";
            case "Rejected": return "bg-soft-danger";
            case "Live": return "bg-soft-primary";
            case "Completed": return "bg-soft-dark";
            case "Canceled": return "bg-soft-warning";
            default: return "bg-soft-light";
        }
    }

    function getInitialView() {
        if (window.innerWidth >= 768 && window.innerWidth < 1200) return 'timeGridWeek';
        if (window.innerWidth <= 768) return 'listMonth';
        return 'dayGridMonth';
    }
    if (calendarEl) {
        var calendar = new FullCalendar.Calendar(calendarEl, {
            timeZone: 'local',
            editable: false,
            selectable: false,
            navLinks: true,
            initialView: getInitialView(),
            themeSystem: 'bootstrap',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay,listMonth'
            },
            windowResize: function () {
                calendar.changeView(getInitialView());
            },
            eventMouseEnter: function (info) {
                info.el.style.cursor = "pointer";
            },
            eventClick: function (info) {
                const props = info.event.extendedProps;

                document.getElementById("detail-trip-name").innerText = info.event.title || "";
                document.getElementById("detail-status").innerText = props.status || "";
                document.getElementById("detail-organization").innerText = props.organizationName || "";
                document.getElementById("detail-organizationCreatorName").innerText = props.organizationCreatorName || "";
                document.getElementById("detail-group").innerText = props.groupName || "";
                document.getElementById("detail-departure-date").innerText = props.departureDate || "";
                document.getElementById("detail-departure-time").innerText = props.departureTime || "";
                document.getElementById("detail-start-location").innerText = props.startLocation || "";
                document.getElementById("detail-destination-date").innerText = props.destinationArrivalDate || "";
                document.getElementById("detail-destination-time").innerText = props.destinationArrivalTime || "";
                document.getElementById("detail-destination-location").innerText = props.destinationLocation || "";
                document.getElementById("detail-passengers").innerText = props.passengers || "";
                document.getElementById("detail-trip-days").innerText = props.tripDays || "";

                // Driver Card
                const driverCardBody = document.querySelector(".driver-card"); // first driver card
                if (props?.driverId) {
                    driverCardBody.innerHTML = `
                <h5 class="card-title">Driver Details</h5>
                <p class="text-muted mb-1 small">Driver ID</p>
                <p class="fw-semibold fs-6">${props?.driver?.driverId || ""}</p>

                <p class="text-muted mb-1 small">Driver Name</p>
                <p class="fw-semibold fs-6">${props?.driver?.driverName || ""}</p>

                <p class="text-muted mb-1 small">Employment Type</p>
                <p class="fw-semibold fs-6">${props?.driver?.employmentType || ""}</p>

                <p class="text-muted mb-1 small">License Number</p>
                <p class="fw-semibold fs-6">${props?.driver?.licenseNumber || ""}</p>

                <p class="text-muted mb-1 small">License Province</p>
                <p class="fw-semibold fs-6">${props?.driver?.licenseProvince || ""}</p>
            `;
                } else {
                    driverCardBody.innerHTML = `<h5 class="card-title">Driver Details</h5>
                  <p class="text-muted">Driver not assigned</p>`;
                }

                // Bus/Equipment Card
                const busCardBody = document.querySelector(".equipment-card"); // second card
                if (props?.manufacturer) {
                    busCardBody.innerHTML = `
                <h5 class="card-title">Bus Details</h5>
                <p class="text-muted mb-1 small">Manufacturer</p>
                <p class="fw-semibold fs-6">${props?.equipment?.manufacturer || ""}</p>

                <p class="text-muted mb-1 small">Modal</p>
                <p class="fw-semibold fs-6">${props?.equipment?.model || ""}</p>

                <p class="text-muted mb-1 small">License Plate</p>
                <p class="fw-semibold fs-6">${props?.equipment?.licensePlate || ""}</p>

                <p class="text-muted mb-1 small">Seating Capacity</p>
                <p class="fw-semibold fs-6">${props?.equipment?.seatingCapacity || ""}</p>

                <p class="text-muted mb-1 small">Year</p>
                <p class="fw-semibold fs-6">${props?.equipment?.year || ""}</p>
            `;
                }
                else {
                    busCardBody.innerHTML = `<h5 class="card-title">Bus Details</h5>
                <p class="text-muted">Equipment not assigned</p>`;
                }



                document.getElementById("detail-notes").innerText = props.notes || "";

                const modal = new bootstrap.Modal(document.getElementById("event-modal"));
                modal.show();
            },
            events: defaultEvents
        });
        calendar.render();
        loadEvents();
    }


    // Upcoming events list
    function renderUpcomingEvents(events) {
        events.sort((a, b) => new Date(a.start) - new Date(b.start));
        const container = document.getElementById("upcoming-event-list");
        container.innerHTML = "";

        events.forEach(event => {
            const startDate = event.start ? formatDate(event.start) : '';
            const endDate = event.end ? " to " + formatDate(event.end) : '';
            const time = event.extendedProps.departureTime || '';

            const html = `
                <div class='card mb-3'>
                    <div class='card-body'>
                        <div class='d-flex mb-3'>
                            <div class='flex-grow-1'>
                                <span class='fw-medium'>${startDate}${endDate}</span>
                            </div>
                            <div class='flex-shrink-0'>
                                <small class='badge badge-soft-primary ms-auto'>${time}</small>
                            </div>
                        </div>
                        <h6 class='card-title fs-16'>${event.title}</h6>
                        <p class='text-muted text-truncate-two-lines mb-0'>${event.extendedProps.notes || ''}</p>
                    </div>
                </div>`;
            container.innerHTML += html;
        });
    }

    function formatDate(date) {
        const d = new Date(date);
        return d.toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" });
    }
});
