/*
Template Name: Chartered_Bus 
Author: Themesbrand
Version: 2.2.0
Website: https://Themesbrand.com/
Contact: Themesbrand@gmail.com
File: Common Plugins Js File
*/

//Common plugins
if (document.querySelectorAll("[toast-list]") || document.querySelectorAll('[data-choices]') || document.querySelectorAll("[data-provider]")) {
    document.writeln("<script type='text/javascript' src='https://cdn.jsdelivr.net/npm/toastify-js'></script>");
    document.writeln("<script type='text/javascript' src='/assets/libs/choices.js/public/assets/scripts/choices.min.js'></script>");
    document.writeln("<script type='text/javascript' src='/assets/libs/flatpickr/flatpickr.min.js'></script>");
}


document.addEventListener("DOMContentLoaded", function () {
    // Hide all validation errors on page load
    document.querySelectorAll(".text-danger").forEach(span => {
        if (!span.innerText.trim()) {
            span.style.display = "none";
        }
    });

    // Show them again when form is submitted
    document.querySelector("form").addEventListener("submit", function () {
        document.querySelectorAll(".text-danger").forEach(span => {
            span.style.display = "inline";
        });
    });
});


$(document).ready(function () {
    $(".customTable td, .customTable th").each(function () {
        let text = $(this).text().trim();
        if (text.length > 20) {
            $(this).attr("title", text);
        }
    });
});
