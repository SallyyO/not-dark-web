import { useEffect, useMemo, useState, type FormEvent } from "react";
import "./index.css";
import { api, type Category, type Customer, type Listing, type Purchase, type WalletTransaction } from "./api/api";

type View = "market" | "sell" | "orders" | "wallet" | "admin";

type CategoryWithCount = Category & { count: number };

const savedUser = typeof localStorage !== "undefined" ? localStorage.getItem("satin-road-user") ?? "" : "";

function money(value: number) {
  return `§${value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function slug(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "");
}

function categoryMatches(listing: Listing, category: Category) {
  return listing.categoryId === category.id || slug(category.name) === listing.categoryId.toLowerCase();
}

export function App() {
  const [view, setView] = useState<View>("market");
  const [listings, setListings] = useState<Listing[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selected, setSelected] = useState<Listing | null>(null);
  const [categoryId, setCategoryId] = useState("all");
  const [query, setQuery] = useState("");
  const [userId, setUserId] = useState(savedUser);
  const [notice, setNotice] = useState("");
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const [listingResponse, categoryResponse, customerResponse] = await Promise.all([
        api.listings.getAll(),
        api.categories.getAll(),
        api.customers.getAll(),
      ]);

      const nextCustomers = customerResponse.data ?? [];
      setListings(listingResponse.data ?? []);
      setCategories(categoryResponse.data ?? []);
      setCustomers(nextCustomers);

      if (!userId && nextCustomers[0]) {
        setUserId(nextCustomers[0].customerId);
        localStorage.setItem("satin-road-user", nextCustomers[0].customerId);
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Could not connect to the marketplace");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const currentUser = customers.find((customer) => customer.customerId === userId);

  const categoryList = useMemo<CategoryWithCount[]>(() => [
    { id: "all", name: "All listings", description: null, count: listings.length },
    ...categories.map((category) => ({
      ...category,
      count: listings.filter((listing) => categoryMatches(listing, category)).length,
    })),
  ], [categories, listings]);

  const filteredListings = useMemo(() => {
    const q = query.trim().toLowerCase();
    const selectedCategory = categories.find((item) => item.id === categoryId);

    return listings.filter((listing) => {
      const matchesCategory = categoryId === "all" || (selectedCategory ? categoryMatches(listing, selectedCategory) : false);
      const matchesSearch = !q || [listing.title, listing.description, listing.categoryId]
        .some((value) => value.toLowerCase().includes(q));
      return matchesCategory && matchesSearch;
    });
  }, [categoryId, categories, listings, query]);

  const chooseUser = (id: string) => {
    setUserId(id);
    localStorage.setItem("satin-road-user", id);
  };

  const buy = async (listing: Listing, quantity: number) => {
    if (!userId) {
      setNotice("Choose an account before buying");
      return;
    }

    try {
      await api.purchases.purchase({ buyerId: userId, listingId: listing.listingId, quantity });
      setSelected(null);
      setNotice(`Order placed for ${listing.title}.`);
      await load();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "The order could not be completed");
    }
  };

  return (
    <div className="site-shell">
      <header className="topbar">
        <button className="brand" onClick={() => setView("market")} aria-label="Satin Road home">
          <span>
            <strong>Satin Road</strong>
            <small>anonymous marketplace</small>
          </span>
        </button>

        <div className="header-actions">
          <button onClick={() => setView("orders")}>messages 0</button>
          <span>|</span>
          <button onClick={() => setView("orders")}>orders 0</button>
          <span>|</span>
          <button onClick={() => setView("wallet")}>account <b>{money(currentUser?.balance ?? 0)}</b></button>
          <select value={userId} onChange={(event) => chooseUser(event.target.value)} aria-label="Account">
            {customers.map((customer) => (
                <option key={customer.customerId} value={customer.customerId}>
                  {customer.username}
                </option>
            ))}
          </select>


        </div>
      </header>

      <div className="search-row">
        <label htmlFor="search">Search</label>
        <div className="search-box">
          <input id="search" value={query} onChange={(event) => setQuery(event.target.value)} />
          <button onClick={() => setQuery(query.trim())}>Go</button>
        </div>
      </div>

      {notice && (
        <div className="notice">
          <span>{notice}</span>
          <button onClick={() => setNotice("")} aria-label="Close">×</button>
        </div>
      )}

      <main className="layout">
        <aside className="sidebar">
          <div className="category-heading">Shop by Category</div>
          {categoryList.map((category) => (
            <button
              key={category.id}
              className={`category-link ${categoryId === category.id ? "active" : ""}`}
              onClick={() => { setCategoryId(category.id); setView("market"); }}
            >
              <span>{category.name}</span>
              <em>{category.count}</em>
            </button>
          ))}

          <div className="sidebar-rule" />
          <button className="utility-link" onClick={() => setView("sell")}>Sell an item</button>
          <button className="utility-link" onClick={() => setView("orders")}>My orders</button>
          <button className="utility-link" onClick={() => setView("wallet")}>My wallet</button>
          <button className="utility-link" onClick={() => setView("admin")}>Administration</button>
        </aside>

        <section className="content">
          {view === "market" && <Marketplace listings={filteredListings} loading={loading} onOpen={setSelected} />}
          {view === "sell" && <SellPage userId={userId} categories={categories} onChanged={load} />}
          {view === "orders" && <OrdersPage userId={userId} />}
          {view === "wallet" && <WalletPage userId={userId} customer={currentUser} onChanged={load} />}
          {view === "admin" && <AdminPage categories={categories} onChanged={load} />}
        </section>
      </main>

      {selected && <ProductModal listing={selected} onClose={() => setSelected(null)} onBuy={buy} />}
    </div>
  );
}

function Marketplace({ listings, loading, onOpen }: { listings: Listing[]; loading: boolean; onOpen: (listing: Listing) => void }) {
  if (loading) return <div className="empty-state">Loading listings...</div>;
  if (!listings.length) return <div className="empty-state">No listings found.</div>;
  return <div className="product-grid">{listings.map((listing) => <ListingCard key={listing.listingId} listing={listing} onOpen={() => onOpen(listing)} />)}</div>;
}

function ListingCard({ listing, onOpen }: { listing: Listing; onOpen: () => void }) {
  const initial = listing.title.trim().charAt(0).toUpperCase() || "?";
  return (
    <button className="product" onClick={onOpen}>
      <div className="product-image"><span>{initial}</span></div>
      <div className="product-title">{listing.title}</div>
      <div className="product-price">{money(listing.price)}</div>
    </button>
  );
}

function ProductModal({ listing, onClose, onBuy }: { listing: Listing; onClose: () => void; onBuy: (listing: Listing, quantity: number) => void }) {
  const [quantity, setQuantity] = useState(1);
  const max = Math.max(1, listing.stock);

  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal" onMouseDown={(event) => event.stopPropagation()}>
        <button className="close" onClick={onClose}>×</button>
        <div className="modal-image"><span>{listing.title.charAt(0).toUpperCase()}</span></div>
        <div className="modal-info">
          <div className="modal-category">{listing.categoryId}</div>
          <h2>{listing.title}</h2>
          <p>{listing.description}</p>
          <div className="modal-price">{money(listing.price)}</div>
          <div className="buy-row">
            <label>Qty <select value={quantity} onChange={(event) => setQuantity(Number(event.target.value))}>{Array.from({ length: max }, (_, i) => <option key={i + 1} value={i + 1}>{i + 1}</option>)}</select></label>
            <button onClick={() => void onBuy(listing, quantity)}>Buy</button>
          </div>
          <small>{listing.stock} available</small>
        </div>
      </div>
    </div>
  );
}

function SellPage({ userId, categories, onChanged }: { userId: string; categories: Category[]; onChanged: () => Promise<void> }) {
  const [mine, setMine] = useState<Listing[]>([]);
  const [editing, setEditing] = useState<string | null>(null);
  const [status, setStatus] = useState("");
  const [form, setForm] = useState({ title: "", description: "", categoryId: "", price: "", stock: "1" });

  const refresh = async () => {
    if (!userId) return setMine([]);
    const result = await api.listings.getForVendor(userId);
    setMine(result.data ?? []);
  };

  useEffect(() => { void refresh(); }, [userId]);

  useEffect(() => {
    if (!form.categoryId && categories[0]) setForm((current) => ({ ...current, categoryId: categories[0].id }));
  }, [categories, form.categoryId]);

  const reset = () => setForm({ title: "", description: "", categoryId: categories[0]?.id ?? "", price: "", stock: "1" });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!userId) return setStatus("Choose an account first.");
    try {
      const body = { vendorId: userId, categoryId: form.categoryId, title: form.title, description: form.description, price: Number(form.price), stock: Number(form.stock) };
      if (editing) await api.listings.update(editing, body); else await api.listings.create(body);
      setStatus(editing ? "Listing updated." : "Listing published.");
      setEditing(null); reset(); await refresh(); await onChanged();
    } catch (error) { setStatus(error instanceof Error ? error.message : "Could not save listing."); }
  };

  const edit = (listing: Listing) => {
    setEditing(listing.listingId);
    setForm({ title: listing.title, description: listing.description, categoryId: listing.categoryId, price: String(listing.price), stock: String(listing.stock) });
  };

  const archive = async (listing: Listing) => {
    try { await api.listings.remove(listing.listingId, userId); setStatus("Listing archived."); await refresh(); await onChanged(); }
    catch (error) { setStatus(error instanceof Error ? error.message : "Could not archive listing."); }
  };

  return (
    <div className="simple-page">
      <h1>{editing ? "Edit listing" : "Sell an item"}</h1>
      {status && <p className="form-message">{status}</p>}
      {!userId ? <p>Choose an account above before creating listings.</p> : (
        <form className="simple-form" onSubmit={submit}>
          <label>Title<input required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label>
          <label>Category<select required value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
          <label>Description<textarea required rows={4} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
          <div className="two-fields"><label>Price<input required type="number" min="0" step="0.01" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} /></label><label>Stock<input required type="number" min="0" step="1" value={form.stock} onChange={(e) => setForm({ ...form, stock: e.target.value })} /></label></div>
          <div><button className="plain-button" type="submit">{editing ? "Save" : "Publish"}</button>{editing && <button className="link-button" type="button" onClick={() => { setEditing(null); reset(); }}>Cancel</button>}</div>
        </form>
      )}

      <h2>My listings</h2>
      {mine.length ? <div className="simple-list">{mine.map((listing) => <div className="simple-list-row" key={listing.listingId}><span>{listing.title} — {money(listing.price)} — {listing.stock} in stock</span><span><button className="link-button" onClick={() => edit(listing)}>edit</button><button className="link-button" onClick={() => void archive(listing)}>archive</button></span></div>)}</div> : <p>No listings for this account.</p>}
    </div>
  );
}

function OrdersPage({ userId }: { userId: string }) {
  const [orders, setOrders] = useState<Purchase[]>([]);
  useEffect(() => { if (userId) void api.purchases.getForBuyer(userId).then((response) => setOrders(response.data ?? [])); }, [userId]);

  return <div className="simple-page"><h1>Orders</h1>{orders.length ? <div className="simple-list">{orders.map((order) => <div className="simple-list-row" key={order.purchaseId}><span>{order.listingId.slice(0, 8)} — {new Date(order.purchasedAt).toLocaleString()}</span><strong>{money(order.totalPrice)}</strong></div>)}</div> : <p>No orders for this account.</p>}</div>;
}

function WalletPage({ userId, customer, onChanged }: { userId: string; customer?: Customer; onChanged: () => Promise<void> }) {
  const [amount, setAmount] = useState("100");
  const [transactions, setTransactions] = useState<WalletTransaction[]>([]);
  const [message, setMessage] = useState("");

  const loadWallet = async () => {
    if (!userId) return;
    const result = await api.customers.getWallet(userId);
    setTransactions(result.data.transactions ?? []);
  };

  useEffect(() => { void loadWallet(); }, [userId]);

  const deposit = async () => {
    try { await api.customers.deposit(userId, Number(amount)); setMessage("Wallet updated."); await onChanged(); await loadWallet(); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Deposit failed."); }
  };

  return <div className="simple-page"><h1>Wallet</h1><p className="balance-line">Balance <strong>{money(customer?.balance ?? 0)}</strong></p><div className="wallet-form"><input type="number" min="1" step="1" value={amount} onChange={(e) => setAmount(e.target.value)} /><button className="plain-button" onClick={() => void deposit()}>Add funds</button></div>{message && <p className="form-message">{message}</p>}<h2>Transactions</h2>{transactions.length ? <div className="simple-list">{transactions.map((transaction) => <div className="simple-list-row" key={transaction.transactionId}><span>{transaction.description}</span><strong className={transaction.amount < 0 ? "negative" : "positive"}>{transaction.amount < 0 ? "−" : "+"}{money(Math.abs(transaction.amount))}</strong></div>)}</div> : <p>No wallet transactions.</p>}</div>;
}

function AdminPage({ categories, onChanged }: { categories: Category[]; onChanged: () => Promise<void> }) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [status, setStatus] = useState("");

  const add = async () => {
    if (!name.trim()) return;
    try { await api.categories.create({ name: name.trim(), description: description.trim() || null }); setName(""); setDescription(""); setStatus("Category added."); await onChanged(); }
    catch (error) { setStatus(error instanceof Error ? error.message : "Could not add category."); }
  };

  const remove = async (id: string) => {
    try { await api.categories.remove(id); setStatus("Category removed."); await onChanged(); }
    catch (error) { setStatus(error instanceof Error ? error.message : "Could not remove category."); }
  };

  return <div className="simple-page"><h1>Categories</h1><p>Create and remove the categories used by listings.</p>{status && <p className="form-message">{status}</p>}<div className="category-form"><input placeholder="Category name" value={name} onChange={(e) => setName(e.target.value)} /><input placeholder="Description (optional)" value={description} onChange={(e) => setDescription(e.target.value)} /><button className="plain-button" onClick={() => void add}>Add</button></div><div className="simple-list">{categories.map((category) => <div className="simple-list-row" key={category.id}><span><strong>{category.name}</strong>{category.description ? ` — ${category.description}` : ""}</span><button className="link-button" onClick={() => void remove(category.id)}>remove</button></div>)}</div></div>;
}

export default App;
