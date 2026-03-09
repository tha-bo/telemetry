<img width="679" height="768" alt="image" src="https://github.com/user-attachments/assets/a7670a93-0a0d-49a0-ac0d-eede9bfa2dd0" />

## Telemetry Viewer

Simple telemetry viewer consisting of:
- **API**: ASP.NET Core minimal API with SQLite storage
- **Frontend**: React + Vite dashboard for exploring telemetry per tenant/device

---

### Prerequisites

- **.NET SDK**: `10.0` or later (to run the API)
- **Node.js**: `>= 18` (recommended) with npm or another package manager

---

### 1. Backend API

From the `api` directory:

```bash
cd api
dotnet restore
dotnet run
```

The API will normally start on `http://localhost:5274` (configured as `API_BASE_URL` in the frontend).

#### (Optional) Reseed the database

With the API running, you can purge and reseed the SQLite database from `seed.json`:

```bash
curl -X POST http://localhost:5274/admin/purge-and-reseed
```

---

### 2. Frontend (React + Vite)

From the `frontend` directory:

```bash
cd frontend
npm install
npm run dev
```

By default Vite serves on `http://localhost:5173`. Open that URL in a browser.

The app expects the API to be running at `http://localhost:5274`. If you change the API port, update `API_BASE_URL` in `frontend/src/App.tsx`.

---

### 3. Using the app

1. Start the **API** (`dotnet run` in `api`)
2. Start the **frontend** (`npm run dev` in `frontend`)
3. Visit `http://localhost:5173`
4. Use the controls at the top to:
   - Select a **tenant**
   - Optionally filter by **device**
   - Choose a **date range** (24h, 1 month, 1 year)

The table, graph, and summary stats (min/max/average) will all update based on your selections.

