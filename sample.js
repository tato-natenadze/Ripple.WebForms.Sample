"use strict";
let posting = false;
let stopped = false;
const renderedRows = new Map();
let currentState = null;
let actionPosition = null;
let previousOverflowAnchor = "";
let busyAction = null;

function initializeTabs() {
    const tabs = Array.from(document.querySelectorAll('.tabs [role="tab"]'));
    const saved = document.getElementById("ActiveTab");
    function select(tab, focus) {
        if (tab.dataset.tab !== saved.value && actionPosition) {
            actionPosition = null;
            document.documentElement.style.overflowAnchor = previousOverflowAnchor;
        }
        tabs.forEach(item => {
            const selected = item === tab;
            item.setAttribute("aria-selected", String(selected));
            item.tabIndex = selected ? 0 : -1;
            document.getElementById(item.getAttribute("aria-controls")).hidden = !selected;
        });
        saved.value = tab.dataset.tab;
        if (tab.dataset.tab === "signing") initializeSigningDefaults();
        if (focus) tab.focus({ preventScroll: true });
    }
    tabs.forEach((tab, index) => {
        tab.addEventListener("click", () => select(tab, false));
        tab.addEventListener("keydown", event => {
            let target;
            if (event.key === "ArrowRight") target = (index + 1) % tabs.length;
            else if (event.key === "ArrowLeft") target = (index + tabs.length - 1) % tabs.length;
            else if (event.key === "Home") target = 0;
            else if (event.key === "End") target = tabs.length - 1;
            else return;
            event.preventDefault();
            select(tabs[target], true);
        });
    });
    select(tabs.find(tab => tab.dataset.tab === saved.value) || tabs[0], false);
}

function initializeExampleTabs() {
    const saved = document.getElementById("ActiveExamples");
    let selections;
    try { selections = JSON.parse(saved.value); } catch { selections = {}; }
    if (!selections || typeof selections !== "object" || Array.isArray(selections)) selections = {};
    document.querySelectorAll(".example-tabs").forEach(group => {
        const tabs = Array.from(group.querySelectorAll('[role="tab"]'));
        const topic = group.dataset.topic;
        function select(tab, focus) {
            const selectedPanel = document.getElementById(tab.getAttribute("aria-controls"));
            if (selectedPanel.hidden && actionPosition) {
                const action = document.getElementById(actionPosition.id);
                if (group.parentElement.contains(action)) {
                    actionPosition = null;
                    document.documentElement.style.overflowAnchor = previousOverflowAnchor;
                }
            }
            tabs.forEach(item => {
                const active = item === tab;
                item.setAttribute("aria-selected", String(active));
                item.tabIndex = active ? 0 : -1;
                document.getElementById(item.getAttribute("aria-controls")).hidden = !active;
            });
            selections[topic] = tab.dataset.view;
            saved.value = JSON.stringify(selections);
            if (focus) tab.focus({ preventScroll: true });
        }
        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => select(tab, false));
            tab.addEventListener("keydown", event => {
                let target;
                if (event.key === "ArrowRight") target = (index + 1) % tabs.length;
                else if (event.key === "ArrowLeft") target = (index + tabs.length - 1) % tabs.length;
                else if (event.key === "Home") target = 0;
                else if (event.key === "End") target = tabs.length - 1;
                else return;
                event.preventDefault();
                select(tabs[target], true);
            });
        });
        select(tabs.find(tab => tab.dataset.view === selections[topic]) || tabs[0], false);
    });
}

function initializeSigningDefaults() {
    const ready = document.getElementById("SigningDefaultsReady");
    if (ready.value === "true") return;
    document.getElementById("SigningAmount").value = document.getElementById("Amount").value;
    document.getElementById("SigningDestination").value = document.getElementById("PaymentDestination").value.trim() ||
        currentState?.Receiver.Address || document.getElementById("SigningDestination").value;
    document.getElementById("SigningTag").value = document.getElementById("PaymentTag").value;
    document.getElementById("SigningMemo").value = document.getElementById("PaymentMemo").value;
    ready.value = "true";
}

function showBusyAction(button) {
    if (!button) return;
    busyAction = { id: button.id, value: button.value, width: button.style.width };
    button.style.width = button.getBoundingClientRect().width + "px";
    button.value = "Working";
    button.setAttribute("aria-busy", "true");
}

function clearBusyAction() {
    if (!busyAction) return;
    const button = document.getElementById(busyAction.id);
    if (button) {
        button.value = busyAction.value;
        button.style.width = busyAction.width;
        button.removeAttribute("aria-busy");
    }
    busyAction = null;
}

function captureActionPosition(button) {
    if (!button) return null;
    const bounds = button.getBoundingClientRect();
    return { id: button.id, top: bounds.top, left: bounds.left, x: window.scrollX, y: window.scrollY };
}

function restoreActionPosition(position) {
    if (!position) return;
    const button = document.getElementById(position.id);
    if (!button) { window.scrollTo(position.x, position.y); return; }
    const bounds = button.getBoundingClientRect();
    window.scrollTo(window.scrollX + bounds.left - position.left, window.scrollY + bounds.top - position.top);
}

function disableActions() {
    document.querySelectorAll('input[type="button"], input[type="submit"]').forEach(button => { button.disabled = true; });
}

function beginAction(button) {
    if (posting) return false;
    // Avoid applying browser scroll anchoring and our position correction twice.
    previousOverflowAnchor = document.documentElement.style.overflowAnchor;
    document.documentElement.style.overflowAnchor = "none";
    actionPosition = captureActionPosition(button);
    posting = true;
    showBusyAction(button);
    disableActions();
    document.getElementById("connection").textContent = "Processing your request…";
    restoreActionPosition(actionPosition);
    return true;
}

function updateActions(state) {
    const disabled = posting || state.Busy || !state.Ready;
    document.querySelectorAll(".tool-action").forEach(button => {
        button.disabled = disabled || button.dataset.available === "false";
    });
    document.getElementById("CreateSender").disabled = disabled || !!state.Sender.Address;
    document.getElementById("CreateReceiver").disabled = disabled || !!state.Receiver.Address;
    document.getElementById("FundSender").disabled = disabled || !state.Sender.Address;
    document.getElementById("Send").disabled = disabled || !state.Sender.Address ||
        (!state.Receiver.Address && !document.getElementById("PaymentDestination").value.trim());
    updateDeleteAvailability();
}

function updateDeleteAvailability() {
    const button = document.getElementById("DeleteAccount");
    const key = JSON.stringify(["DeleteSource", "DeleteDestination", "DeleteTag", "DeleteMaxFee"]
        .map(id => document.getElementById(id).value.trim()));
    button.disabled = posting || !currentState || currentState.Busy || !currentState.Ready ||
        button.dataset.available !== "true" || button.dataset.quote !== key;
}

document.addEventListener("input", event => {
    if (["DeleteSource", "DeleteDestination", "DeleteTag", "DeleteMaxFee"].includes(event.target.id)) updateDeleteAvailability();
});
document.addEventListener("change", event => {
    if (event.target.id === "DeleteSource") updateDeleteAvailability();
});

function wallet(prefix, value) {
    document.getElementById(prefix + "Address").textContent = value.Address || "Not created";
    document.getElementById(prefix + "Seed").textContent = value.Seed || "Not created";
    document.getElementById(prefix + "Balance").textContent = value.Balance;
    document.getElementById(prefix + "Reserve").textContent = value.Reserve || "—";
    document.getElementById(prefix + "Available").textContent = value.Available || "—";
    document.getElementById(prefix + "Status").textContent = value.Status + (value.Ledger ? " · ledger " + value.Ledger : "");
}

function rows(id, values, fields, emptyMessage = "No payments yet.") {
    const serialized = JSON.stringify(values);
    if (renderedRows.get(id) === serialized) return;
    renderedRows.set(id, serialized);
    const body = document.getElementById(id);
    body.replaceChildren();
    if (values.length === 0) {
        const row = body.insertRow();
        const cell = row.insertCell();
        cell.colSpan = fields.length;
        cell.textContent = emptyMessage;
        return;
    }
    values.forEach(value => {
        const row = body.insertRow();
        fields.forEach(field => {
            const cell = row.insertCell();
            cell.textContent = value[field] === null || value[field] === undefined ? "—" :
                field === "Error" ? JSON.stringify(value[field], null, 2) : String(value[field]);
            if (field === "Hash") cell.className = "hash";
            if (field === "Error") cell.className = "diagnostic";
        });
    });
}

async function refresh() {
    if (stopped) return;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    try {
        const response = await fetch("Status.ashx", { cache: "no-store", credentials: "same-origin", signal: controller.signal });
        const state = await response.json();
        if (!response.ok) throw new Error(state.Error || "The sample is unavailable.");
        currentState = state;
        document.getElementById("currentServerName").textContent = state.Server.Name;
        document.getElementById("currentRpcEndpoint").textContent = state.Server.RpcEndpoint;
        document.getElementById("currentSocketEndpoint").textContent = state.Server.WebSocketEndpoint;
        wallet("sender", state.Sender);
        wallet("receiver", state.Receiver);
        if (!posting) document.getElementById("connection").textContent = state.Message;
        document.getElementById("monitoring").textContent = state.Monitoring;
        document.getElementById("feeEstimate").textContent = state.EstimatedFee || "—";
        rows("transfers", state.Transfers, ["Amount", "Status", "Fee", "Hash", "Detail", "DestinationTag", "Memo", "Destination", "Error"]);
        rows("incoming", state.Incoming, ["Amount", "Hash", "Ledger"]);
        rows("accountDeletes", state.AccountDeletes, ["Status", "Returned", "Fee", "Sender", "Destination", "DestinationTag", "Hash", "Detail", "Error"], "No account deletions yet.");
        updateActions(state);
    } catch (error) {
        disableActions();
        if (!posting) document.getElementById("connection").textContent = error.name === "AbortError" ? "The status request timed out." : error.message;
    } finally {
        clearTimeout(timeout);
        if (!stopped) setTimeout(refresh, 2000);
    }
}

if (typeof Sys !== "undefined" && Sys.WebForms) {
    const requests = Sys.WebForms.PageRequestManager.getInstance();
    requests.add_pageLoading(() => {
        // Capture again immediately before replacement, preserving any manual
        // scrolling while the request was in flight.
        if (actionPosition) actionPosition = captureActionPosition(document.getElementById(actionPosition.id));
    });
    requests.add_endRequest((sender, args) => {
        posting = false;
        clearBusyAction();
        const error = args.get_error();
        if (error) {
            args.set_errorHandled(true);
            document.getElementById("connection").textContent = error.message;
        }
        if (currentState) updateActions(currentState);
        restoreActionPosition(actionPosition);
        document.documentElement.style.overflowAnchor = previousOverflowAnchor;
        actionPosition = null;
    });
}

window.addEventListener("pagehide", () => { stopped = true; });
initializeTabs();
initializeExampleTabs();
disableActions();
refresh();
