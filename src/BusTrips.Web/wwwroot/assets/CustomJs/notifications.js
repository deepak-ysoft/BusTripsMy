let notificationsUrl = '/Account/Notifications';
let notificationDetailsUrl = '/Account/NotificationDetails';
let connection;

// ------------------- Global Unread Count -------------------
let unreadCount = 0;
const countBadge = document.getElementById("notificationCount");

// ------------------- Badge Update Helper -------------------
function updateBadge(count) {
    if (!countBadge) return;

    if (count > 0) {
        countBadge.innerText = count;
        countBadge.style.display = "inline-block";
    } else {
        countBadge.innerText = "";
        countBadge.style.display = "none";
    }
}

// ------------------- Load Notifications on Page Load -------------------
function loadNotifications() {
    fetch(notificationsUrl)
        .then(res => res.json())
        .then(result => {
            const notifications = result.data || [];
            unreadCount = notifications.filter(n => !n.isRead).length; // update global count
            updateBadge(unreadCount);

            const list = document.getElementById("notificationList");
            const noText = document.getElementById("noNotificationText");

            if (list) {
                list.innerHTML = "";
                if (notifications.length > 0) {
                    notifications.forEach(n => appendNotification(n, false, list));
                    if (noText) noText.style.display = "none";
                } else if (noText) {
                    noText.style.display = "block";
                }
            }
        })
        .catch(err => console.error("Failed to load notifications:", err));
}

// ------------------- Append Notification to List -------------------
function appendNotification(data, prepend = false, listContainer = null) {
    if (!listContainer) return;

    const item = document.createElement("div");
    item.className = `list-group-item list-group-item-action d-flex align-items-start justify-content-between notification-item ${data.isRead ? "" : "fw-bold bg-light"}`;
    item.dataset.id = data.id;

    const content = document.createElement("div");
    content.className = "notification-list-item";

    // Title (one line)
    const titleDiv = document.createElement("div");
    titleDiv.className = "notification-title text-truncate";
    titleDiv.innerText = data.title;
    titleDiv.title = data.title;

    // Date (one line)
    const dateDiv = document.createElement("div");
    dateDiv.className = "notification-date text-truncate";
    const dateObj = new Date(data.date);
    dateDiv.innerText = dateObj.toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

    // Message (one line)
    const msgDiv = document.createElement("div");
    msgDiv.className = "notification-message text-truncate";
    msgDiv.innerText = data.message;
    msgDiv.title = data.message;

    content.appendChild(titleDiv);
    content.appendChild(dateDiv);
    content.appendChild(msgDiv);

    // Icon
    const icon = document.createElement("div");
    icon.className = `notification-icon rounded-circle ${data.isRead ? "bg-secondary" : "bg-primary"} text-white`;
    icon.innerHTML = `<i class="mdi mdi-bell-outline"></i>`;

    item.appendChild(content);
    item.appendChild(icon);

    // Click event to open details
    item.addEventListener("click", () => openNotification(data.id, item));

    if (prepend) listContainer.prepend(item);
    else listContainer.appendChild(item);
}

// ------------------- Open Notification Details -------------------
function openNotification(id, itemElement = null) {
    const content = document.getElementById("notificationContent");
    const container = document.getElementById("notificationsContainer");

    fetch(`${notificationDetailsUrl}?id=${id}`)
        .then(res => res.text())
        .then(html => {
            const detailsHtml = `
                <div id="detailsHeader">
                    <button id="backToListBtn">
                        <i class="mdi mdi-arrow-left"></i> Back
                    </button>
                </div>
                <div>${html}</div>
            `;
            content.innerHTML = detailsHtml;

            // Mark notification as read visually and update badge
            if (itemElement && itemElement.classList.contains("fw-bold")) {
                itemElement.classList.remove("fw-bold", "bg-light");
                const icon = itemElement.querySelector(".notification-icon");
                if (icon) {
                    icon.classList.remove("bg-primary");
                    icon.classList.add("bg-secondary");
                }

                // Decrement global unread count
                unreadCount = Math.max(0, unreadCount - 1);
                updateBadge(unreadCount);

                // Optional: mark as read on server if API exists
            }

            // Slide animation for mobile
            if (window.innerWidth < 992 && container) {
                container.classList.add("show-details");
                setTimeout(() => {
                    window.scrollTo(0, 0);
                    document.body.scrollTop = 0;
                    document.documentElement.scrollTop = 0;
                }, 350);
            }

            // Back button
            const backBtn = document.getElementById("backToListBtn");
            if (backBtn) {
                backBtn.addEventListener("click", () => {
                    container.classList.remove("show-details");
                });
            }
        })
        .catch(err => console.error("Failed to load notification details:", err));
}

// ------------------- Initialize SignalR -------------------
function initSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    connection.start().catch(err => console.error(err.toString()));

    connection.on("ReceiveNotification", function (data) {
        const list = document.getElementById("notificationList");
        if (list) appendNotification(data, true, list);

        const noText = document.getElementById("noNotificationText");
        if (noText) noText.style.display = "none";

        // Increment global unread count
        unreadCount += 1;
        updateBadge(unreadCount);
    });
}

// ------------------- Init -------------------
document.addEventListener("DOMContentLoaded", function () {
    loadNotifications();
    initSignalR();
});

function copyNotificationText(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const text = el.innerText || el.textContent;
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            showAlert("Copied", true);
        });
    } else {
        // Fallback for older browsers
        const textarea = document.createElement("textarea");
        textarea.value = text;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand("copy");
        document.body.removeChild(textarea);
        showAlert("Copied", true);
    }
}