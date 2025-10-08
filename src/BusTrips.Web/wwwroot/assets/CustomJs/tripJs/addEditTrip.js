// ------------------- Helper Functions -------------------
function parseDate(id) {
    let val = document.getElementById(id)?.value;
    return val ? new Date(val) : null;
}

function parseDateTime(dateId, timeId) {
    let d = document.getElementById(dateId)?.value;
    let t = document.getElementById(timeId)?.value;
    if (!d || !t) return null;
    return new Date(d + "T" + t);
}

function formatDate(date) {
    let d = date.getDate().toString().padStart(2, "0");
    let m = (date.getMonth() + 1).toString().padStart(2, "0");
    let y = date.getFullYear();
    return `${y}-${m}-${d}`;
}

// ------------------- Validation Helpers -------------------
function setValidationMessage(inputId, message) {
    let span = document.querySelector(`[data-valmsg-for="${inputId}"]`) ||
        document.querySelector(`#${inputId} ~ .text-danger`);
    if (span) span.textContent = message;
}

function clearValidationMessage(inputId) {
    setValidationMessage(inputId, "");
}



// ------------------- Date Calculations -------------------
function updateTripDaysFromDestination() {
    let departureDate = parseDate("DepartureDate");
    let arrivalDate = parseDate("DestinationArrivalDate");

    if (!departureDate || !arrivalDate) return;

    let diffMs = arrivalDate - departureDate;
    let diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    let currentDays = parseInt(document.getElementById("TripDays")?.value) || 0;

    // Only update TripDays if gap is bigger
    if (diffDays > currentDays) {
        document.getElementById("TripDays").value = diffDays;
        clearValidationMessage("TripDays");
    }
}

// ------------------- AJAX Submit -------------------
$(document).on("submit", "#tripForm", function (e) {
    e.preventDefault();
    const $form = $(this);
    const btn = $form.find("button[type='submit']");
    btn.prop("disabled", true);

    const isEdit = $form.data("isedit");
    const formData = $form.serialize();
    const controller = $form.find("input[name='controller']").val();
    const Id = $form.find("input[name='Id']").val();

    const url = isEdit == "True" || Id != undefined ? `/${controller}/EditTrip` : `/${controller}/CreateTrip`;
    $.post(url, formData)
        .always(() => btn.prop("disabled", false))
        .done(result => {
            if (result.success) {
                $("#shared-alert").remove();
                $("body").prepend(`
                    <div id="shared-alert" class="alert alert-success alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow px-5" style="z-index:9999;">
                        ${result.message}
                        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                    </div>
                `);
                if (result.redirectUrl) window.location.href = result.redirectUrl;
            } else {
                if (result.errors) {
                    focusFirstInvalidField(result.errors, "#trip-form-container");
                }
                else if (result.message != undefined) {
                    showAlert(result.message, false);
                }
                else {
                    $("#trip-form-container").html(result);
                }
            }
        })
        .fail(() => alert("Something went wrong. Please try again."));
});

function updateDestinationFromTripDays() {
    let departureDate = parseDate("DepartureDate");
    let tripDays = parseInt(document.getElementById("TripDays")?.value);

    if (!departureDate || isNaN(tripDays)) return;

    let newArrival = new Date(departureDate);
    newArrival.setDate(newArrival.getDate() + tripDays);
    document.getElementById("DestinationArrivalDate").value = formatDate(newArrival);

    clearValidationMessage("DestinationArrivalDate");
}

function validateDates() {
    let now = new Date();
    let departureDateTime = parseDateTime("DepartureDate", "DepartureTime");
    let arrivalDateTime = parseDateTime("DestinationArrivalDate", "DestinationArrivalTime");

    let isValid = true;

    // Departure must be future
    if (departureDateTime && departureDateTime <= now) {
        setValidationMessage("DepartureDate", "Departure date/time must be in the future.");
        isValid = false;
    } else {
        clearValidationMessage("DepartureDate");
    }

    // Destination must be after departure
    if (arrivalDateTime && departureDateTime && arrivalDateTime <= departureDateTime) {
        setValidationMessage("DestinationArrivalDate", "Arrival date/time must be later than departure.");
        isValid = false;
    } else {
        clearValidationMessage("DestinationArrivalDate");
    }

    return isValid;
}

// ------------------- Event Listeners -------------------

// TripDays change → update DestinationArrival
document.getElementById("TripDays")?.addEventListener("input", () => {
    updateDestinationFromTripDays();
    validateDates();
});

// DestinationArrivalDate change → only validate + update TripDays if gap bigger
document.getElementById("DestinationArrivalDate")?.addEventListener("input", () => {
    updateTripDaysFromDestination();
    validateDates();
});

// DepartureDate / DepartureTime change → recalc DestinationArrival based on TripDays + validate
["DepartureDate", "DepartureTime"].forEach(id => {
    document.getElementById(id)?.addEventListener("input", () => {
        updateDestinationFromTripDays();
        updateTripDaysFromDestination();
        validateDates();
    });
});

// DestinationArrivalTime change → only validate
document.getElementById("DestinationArrivalTime")?.addEventListener("input", () => {
    validateDates();
});
