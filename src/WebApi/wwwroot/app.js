const api = "api/educations";
const $ = (id) => document.getElementById(id);
let records = [];
let sort = { key: "school", dir: 1 };
let editingId = null;

function showError(msg) {
  const el = $("error");
  el.textContent = msg || "";
  el.hidden = !msg;
}

async function request(url, options = {}) {
  const res = await fetch(url, { headers: { "Content-Type": "application/json" }, ...options });
  if (!res.ok) {
    let detail = "";
    try {
      const body = await res.json();
      detail = body.errors ? Object.values(body.errors).flat().join(" ") : (body.title || "");
    } catch { /* no body */ }
    throw new Error(`${res.status} ${res.statusText}${detail ? ": " + detail : ""}`);
  }
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

async function checkHealth() {
  const el = $("health");
  try {
    const res = await fetch("health");
    const text = await res.text();
    el.textContent = res.ok ? `API ${text.toLowerCase()}` : `API ${text || res.status}`;
    el.className = "health " + (res.ok ? "ok" : "bad");
  } catch {
    el.textContent = "API unreachable";
    el.className = "health bad";
  }
}

let loadSeq = 0;

async function load() {
  const seq = ++loadSeq;
  let next = [];
  let error = "";
  try {
    next = (await request(api)) || [];
  } catch (e) {
    error = `Could not load records (${e.message}).`;
  }
  if (seq !== loadSeq) return;
  records = next;
  showError(error);
  render();
}

function render() {
  const q = $("search").value.trim().toLowerCase();
  const rows = records
    .filter((r) => !q || [r.degree, r.fieldOfStudy, r.school, r.description].some((v) => (v || "").toLowerCase().includes(q)))
    .sort((a, b) => sort.dir * (a[sort.key] || "").localeCompare(b[sort.key] || ""));

  $("stat-total").textContent = records.length;
  $("stat-schools").textContent = new Set(records.map((r) => r.school)).size;
  $("stat-fields").textContent = new Set(records.map((r) => r.fieldOfStudy)).size;

  document.querySelectorAll("th[data-sort]").forEach((th) => {
    th.classList.toggle("asc", th.dataset.sort === sort.key && sort.dir === 1);
    th.classList.toggle("desc", th.dataset.sort === sort.key && sort.dir === -1);
    th.setAttribute("aria-sort", th.dataset.sort !== sort.key ? "none" : sort.dir === 1 ? "ascending" : "descending");
  });

  const tbody = $("rows");
  tbody.replaceChildren(...rows.map(rowFor));
  $("empty").hidden = rows.length > 0;
  $("empty").textContent = records.length ? "No records match the filter." : "No records yet. Use “Add record” to create one.";
}

function rowFor(r) {
  const tr = document.createElement("tr");
  const cell = (text, cls) => {
    const td = document.createElement("td");
    td.textContent = text || "";
    if (cls) td.className = cls;
    tr.appendChild(td);
  };
  cell(r.degree);
  cell(r.fieldOfStudy);
  cell(r.school);
  cell(r.description, "desc");
  cell(r.id.slice(0, 8), "id");
  const actions = document.createElement("td");
  actions.className = "actions";
  const edit = document.createElement("button");
  edit.className = "link";
  edit.textContent = "Edit";
  edit.onclick = () => openDialog(r);
  const del = document.createElement("button");
  del.className = "link danger";
  del.textContent = "Delete";
  del.onclick = () => remove(r);
  actions.append(edit, del);
  tr.appendChild(actions);
  return tr;
}

function showFormError(msg) {
  const el = $("form-error");
  el.textContent = msg || "";
  el.hidden = !msg;
}

function openDialog(record) {
  editingId = record ? record.id : null;
  showFormError("");
  $("dialog-title").textContent = record ? "Edit record" : "Add record";
  const form = $("form");
  form.reset();
  for (const k of ["degree", "fieldOfStudy", "school", "description"]) form.elements[k].value = record ? record[k] || "" : "";
  $("dialog").showModal();
  form.elements.degree.focus();
}

async function save(ev) {
  ev.preventDefault();
  const form = $("form");
  for (const k of ["degree", "fieldOfStudy", "school"]) form.elements[k].value = form.elements[k].value.trim();
  if (!form.reportValidity()) return;
  const body = {
    id: editingId || "00000000-0000-0000-0000-000000000000",
    degree: form.elements.degree.value.trim(),
    fieldOfStudy: form.elements.fieldOfStudy.value.trim(),
    school: form.elements.school.value.trim(),
    description: form.elements.description.value.trim() || null,
  };
  $("save").disabled = true;
  try {
    if (editingId) await request(`${api}/${editingId}`, { method: "PUT", body: JSON.stringify(body) });
    else await request(api, { method: "POST", body: JSON.stringify(body) });
    $("dialog").close();
    await load();
  } catch (e) {
    showFormError(`Could not save record (${e.message}).`);
  } finally {
    $("save").disabled = false;
  }
}

async function remove(r) {
  if (!confirm(`Delete “${r.degree} in ${r.fieldOfStudy}” at ${r.school}?`)) return;
  try {
    await request(`${api}/${r.id}`, { method: "DELETE" });
    await load();
  } catch (e) {
    showError(`Could not delete record (${e.message}).`);
  }
}

$("add").onclick = () => openDialog(null);
$("cancel").onclick = () => $("dialog").close();
$("form").addEventListener("submit", save);
$("search").addEventListener("input", render);
document.querySelectorAll("th[data-sort]").forEach((th) => {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "sort";
  button.textContent = th.textContent;
  button.addEventListener("click", () => {
    sort = { key: th.dataset.sort, dir: sort.key === th.dataset.sort ? -sort.dir : 1 };
    render();
  });
  th.replaceChildren(button);
});

checkHealth();
load();
