import './App.css';
import { useEffect, useMemo, useState } from 'react';
import { getInquiries, updateStatus, archiveInquiry, getInquiry, createInquiry } from './api';

const STATUSES = ['New', 'Contacted', 'Pending', 'Registered', 'Closed', 'Archived'];
const COURSES = [
  { id: 1, name: 'PILATES Level 1' },
  { id: 2, name: 'PILATES Level 2' },
  { id: 3, name: 'PILATES Level 3' },
];


function App() {
  const [inquiries, setInquiries] = useState([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [selected, setSelected] = useState(null);
  const [statusChangeList, setstatusChangeList] = useState([]);
  const [validationMessage, setValidationMessage] = useState([]);
  const [sortKey, setSortKey] = useState('');

  // Fetch all inquiries (optionally filtered by status).
  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await getInquiries(statusFilter);
      setInquiries(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const handleStatusChange = (id, status) => {
    // Reflect the change in the table right away (the select is controlled by i.status).
    setInquiries((prev) =>
      prev.map((i) => (i.id === id ? { ...i, status } : i))
    );
    setstatusChangeList((prev) => {
      const next = prev.filter(([changeId]) => changeId !== id);
      next.push([id, status]);
      return next;
    });
  };

  const handleArchive = async (id) => {
    try {
      await archiveInquiry(id);
      if (selected && selected.id === id) setSelected(null);

    } catch (err) {
      console.log(error)
      setError(`Failed to delete id : ${id}`)
    } finally {
      await load();
    }
  };

  const handleSaveChange = async () => {
    const remaining = [...statusChangeList];
    try {
      while (remaining.length > 0) {
        const [id, status] = remaining[remaining.length - 1];
        await updateStatus(id, status); // throws -> we stop before pop()
        remaining.pop();
      }
      setstatusChangeList([]);
    } catch (error) {
      console.log(error)
      const notUpdated = remaining.map(([id]) => id).join(', ');
      setError(`Failed to save. Not updated yet: ${notUpdated}`);
    } finally {
      await load();
    }
  }

  const handleGetInquiryById = async (id) => {
    try {
      let data = await getInquiry(id)
      setSelected(data)
    } catch (error) {
      console.log(error)
      setError(`Failed to get id ${id} `);
    }
  }

  const handleSubmitInquiry = async (e) => {
    e.preventDefault();
    const form = e.target;
    setError('');
    setValidationMessage([])
    if (validation(form)) {
      const payload = {
        firstName: form.firstName.value,
        lastName: form.lastName.value,
        email: form.email.value,
        phone: form.phone.value,
        courseId: Number(form.courseId.value),
        preferredLocation: form.preferredLocation.value,
        message: form.message.value,
      };
      try {
        await createInquiry(payload);
        form.reset();
        await load();
      } catch (error) {
        console.log(error)
        setError(`Failed to create Inquiry `);
      }
    }
  }
  const validation = (form) => {
    const messages = [];

    const firstName = form.firstName.value.trim();
    const lastName = form.lastName.value.trim();
    const email = form.email.value.trim();
    const phone = form.phone.value.trim();
    const courseId = form.courseId.value;
    const preferredLocation = form.preferredLocation.value.trim();
    const message = form.message.value.trim();

    if (!firstName) {
      messages.push('First name is required.');
    } else if (firstName.length > 100) {
      messages.push('First name must be 100 characters or fewer.');
    }

    if (!lastName) {
      messages.push('Last name is required.');
    } else if (lastName.length > 100) {
      messages.push('Last name must be 100 characters or fewer.');
    }

    if (!email) {
      messages.push('Email is required.');
    } else if (email.length > 256) {
      messages.push('Email must be 256 characters or fewer.');
    }

    if (!phone) {
      messages.push('Phone is required.');
    } else if (phone.length > 50) {
      messages.push('Phone must be 50 characters or fewer.');
    }

    if (!courseId) {
      messages.push('Please select a course.');
    }

    if (preferredLocation.length > 200) {
      messages.push('Preferred location must be 200 characters or fewer.');
    }

    if (message.length > 2000) {
      messages.push('Message must be 2000 characters or fewer.');
    }

    setValidationMessage(messages);
    return messages.length === 0;
  }

  const courseName = (id) => {
    let courseObject = COURSES.find((c) => c.id === Number(id))
    if (courseObject) {
      return courseObject.name
    }
    return ""
  }

  // Each sortable column maps to the value used for comparison.
  const SORT_VALUE = {
    name: (i) => `${i.firstName} ${i.lastName}`.trim().toLowerCase(),
    email: (i) => (i.email || '').toLowerCase(),
    course: (i) => courseName(i.courseId).toLowerCase(),
    location: (i) => (i.preferredLocation || '').toLowerCase(),
    status: (i) => (i.status || '').toLowerCase(),
    created: (i) => new Date(i.createdDate).getTime(),
  };

  // Columns the user can sort by, shown in the sort dropdown above the table.
  const SORT_OPTIONS = [
    { value: '', label: 'Default (unsorted)' },
    { value: 'name', label: 'Name A-Z' },
    { value: 'email', label: 'Email A-Z' },
    { value: 'course', label: 'Course A-Z' },
    { value: 'location', label: 'Location A-Z' },
    { value: 'status', label: 'Status A-Z' },
    { value: 'created', label: 'Created A-Z' },
  ];

  const sortedInquiries = useMemo(() => {
    if (!sortKey || !SORT_VALUE[sortKey]) return inquiries;
    const getValue = SORT_VALUE[sortKey];
    return [...inquiries].sort((a, b) => {
      const av = getValue(a);
      const bv = getValue(b);
      if (av < bv) return -1;
      if (av > bv) return 1;
      return 0;
    });
  }, [inquiries, sortKey]);

  return (
    <div>
      <section className="card">
        <h2 className="card-title">Add new inquiry</h2>
        <form className="inquiry-form" onSubmit={handleSubmitInquiry}>
          <div className="field">
            <label htmlFor="firstName">First Name</label>
            <input required id='firstName' type='text' name='firstName'></input>
          </div>
          <div className="field">
            <label htmlFor="lastName">Last Name</label>
            <input required id='lastName' type='text' name='lastName'></input>
          </div>
          <div className="field">
            <label htmlFor="email">Email</label>
            <input required id='email' type='email' name='email'></input>
          </div>
          <div className="field">
            <label htmlFor="phone">Phone</label>
            <input required id='phone' type='tel' name='phone'></input>
          </div>
          <div className="field">
            <label htmlFor="courseId">Course</label>
            <select required id='courseId' name='courseId'>
              <option value=""> --select a course--</option>
              {COURSES.map((c) => (
                <option key={c.id} value={c.id}>{c.name} course</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor="preferredLocation">Preferred Location</label>
            <input id='preferredLocation' type='text' name='preferredLocation'></input>
          </div>
          <div className="field field-full">
            <label htmlFor="message-input">Message</label>
            <textarea id='message-input' name='message' rows="3"></textarea>
          </div>
          {validationMessage.length > 0 && (
            <ul className="field-full validation-errors" role="alert">
              {validationMessage.map((msg, idx) => (
                <li key={idx}>{msg}</li>
              ))}
            </ul>
          )}
          <div className="field-full form-actions">
            <button type="submit">Add inquiry</button>
          </div>
        </form>
      </section>
      <section className="controls" >
        <div className="field">
          <label htmlFor="status-filter">Filter by status</label>
          <select
            id="status-filter"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="">All (excludes archived)</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
        <button id="refresh-btn" type="button" onClick={load}>Refresh</button>
      </section>

      <div id="message" className="message" role="status" aria-live="polite">
        {error ? `Error: ${error}` : ''}
      </div>

      <section>
        <label htmlFor="status-filter">List of inquiries</label>
        <div className="controls table-controls">
          <div className="field">
            <label htmlFor="sort-by">Sort by</label>
            <select
              id="sort-by"
              value={sortKey}
              onChange={(e) => setSortKey(e.target.value)}
            >
              {SORT_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
        </div>
        <table>
          <caption className="sr-only">List of course inquiries</caption>
          <thead>
            <tr>
              <th scope="col" aria-sort={sortKey === 'name' ? 'ascending' : 'none'}>Name</th>
              <th scope="col" aria-sort={sortKey === 'email' ? 'ascending' : 'none'}>Email</th>
              <th scope="col" aria-sort={sortKey === 'course' ? 'ascending' : 'none'}>Course</th>
              <th scope="col" aria-sort={sortKey === 'location' ? 'ascending' : 'none'}>Location</th>
              <th scope="col" aria-sort={sortKey === 'status' ? 'ascending' : 'none'}>Status</th>
              <th scope="col" aria-sort={sortKey === 'created' ? 'ascending' : 'none'}>Created</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody id="inquiry-rows">

            {loading ? (
              <tr><td colSpan="7">Loading&hellip;</td></tr>
            ) : inquiries.length === 0 ? (
              <tr><td colSpan="7">No inquiries found.</td></tr>
            ) : (
              sortedInquiries.map((i) => (
                <tr key={i.id}>
                  <td>{i.firstName} {i.lastName}</td>
                  <td>{i.email}</td>
                  <td>{courseName(i.courseId)}</td>
                  <td>{i.preferredLocation || '—'}</td>
                  <td>

                    <select
                      value={i.status}
                      onChange={(e) => handleStatusChange(i.id, e.target.value)}
                    >
                      {STATUSES.map((s) => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </td>
                  <td>{new Date(i.createdDate).toLocaleString()}</td>
                  <td>
                    <button type="button" onClick={() => handleGetInquiryById(i.id)}>View</button>
                    <button type="button" onClick={() => handleArchive(i.id)}>Delete</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
        <button type="button" onClick={() => handleSaveChange()}> Save Change</button>
      </section>

      {selected && (
        <section id="details" className="details" >
          <label htmlFor="status-filter">Inquery Detail</label>
          <dl id="details-content">
            <dt>Name</dt><dd>{selected.firstName} {selected.lastName}</dd>
            <dt>Email</dt><dd>{selected.email}</dd>
            <dt>Phone</dt><dd>{selected.phone || '—'}</dd>
            <dt>Course</dt><dd>{courseName(selected.courseId)}</dd>
            <dt>Location</dt><dd>{selected.preferredLocation || '—'}</dd>
            <dt>Message</dt><dd>{selected.message || '—'}</dd>
            <dt>Status</dt><dd>{selected.status}</dd>
            <dt>Created</dt><dd>{new Date(selected.createdDate).toLocaleString()}</dd>
          </dl>
          <button id="details-close" type="button" onClick={() => setSelected(null)}>Close</button>
        </section>
      )}
    </div>
  );
}

export default App;
