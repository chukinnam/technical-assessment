const BASE_URL =  'http://localhost:5000';

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}/api${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': 'testapiKey',
    }
  });

  if (!res.ok) {
    throw new Error(`Request failed (${res.status} ${res.statusText})`);
  }
  return res.status === 204 ? null : res.json();
}
//POST /api/Inquiries
export function createInquiry(data){
  return request(`/inquiries`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
// GET /api/inquiries[?status=New]
export function getInquiries(status) {
  const query = status ? `?status=${encodeURIComponent(status)}` : '';
  return request(`/inquiries${query}`);
}

// GET /api/inquiries/{id}
export function getInquiry(id) {
  return request(`/inquiries/${id}`);
}

// PUT /api/inquiries/{id}/status
export function updateStatus(id, status) {
  return request(`/inquiries/${id}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status }),
  });
}

// DELETE /api/inquiries/{id}  (soft delete -> Archived)
export function archiveInquiry(id) {
  return request(`/inquiries/${id}`, { method: 'DELETE' });
}
