/**
 * Typed API facade for the Satin Road frontend.
 *
 * The backend OpenAPI document can be used to regenerate the low-level client:
 *   bun run api:generate
 *
 * Keeping this small facade in front of the generated client means the React UI
 * stays independent from generated method names while still using the same
 * OpenAPI contract.
 */

export type Listing = {
  listingId: string;
  vendorId: string;
  categoryId: string;
  title: string;
  description: string;
  price: number;
  stock: number;
  isActive: boolean;
};

export type Customer = {
  customerId: string;
  username: string;
  email: string;
  balance: number;
};

export type Category = {
  id: string;
  name: string;
  description?: string | null;
};

export type Purchase = {
  purchaseId: string;
  buyerId: string;
  vendorId: string;
  listingId: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  purchasedAt: string;
};

export type WalletTransaction = {
  transactionId: string;
  customerId: string;
  amount: number;
  type: string;
  description: string;
  createdAt: string;
};

export type CreateListingRequest = {
  vendorId: string;
  categoryId: string;
  title: string;
  description: string;
  price: number;
  stock: number;
};

export type CategoryRequest = {
  name: string;
  description?: string | null;
};

async function request<T>(path: string, init?: RequestInit): Promise<{ data: T }> {
  const headers = new Headers(init?.headers);
  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(path, {
    ...init,
    headers,
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") ?? "";
    let message = `${response.status} ${response.statusText}`;

    try {
      if (contentType.includes("application/json")) {
        const body = await response.json();
        message = body?.detail ?? body?.title ?? body?.message ?? JSON.stringify(body);
      } else {
        message = (await response.text()) || message;
      }
    } catch {
      // Keep the HTTP status as the error message if the body is not readable.
    }

    throw new Error(message);
  }

  if (response.status === 204) return { data: undefined as T };
  return { data: (await response.json()) as T };
}

export const api = {
  customers: {
    getAll: () => request<Customer[]>("/api/customers"),
    getWallet: (id: string) => request<{ customerId: string; balance: number; transactions: WalletTransaction[] }>(`/api/customers/${id}/wallet`),
    deposit: (id: string, amount: number) =>
      request<{ customerId: string; balance: number }>(`/api/customers/${id}/wallet/deposit`, {
        method: "POST",
        body: JSON.stringify({ amount }),
      }),
  },

  categories: {
    getAll: () => request<Category[]>("/categories"),
    get: (id: string) => request<Category>(`/categories/${encodeURIComponent(id)}`),
    create: (body: CategoryRequest) => request<Category>("/categories", { method: "POST", body: JSON.stringify(body) }),
    update: (id: string, body: CategoryRequest) => request<Category>(`/categories/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) }),
    remove: (id: string) => request<void>(`/categories/${encodeURIComponent(id)}`, { method: "DELETE" }),
  },

  listings: {
    getAll: () => request<Listing[]>("/api/listings"),
    getForVendor: (vendorId: string) => request<Listing[]>(`/api/listings/vendor/${vendorId}`),
    create: (body: CreateListingRequest) => request<Listing>("/api/listings", { method: "POST", body: JSON.stringify(body) }),
    update: (id: string, body: CreateListingRequest) => request<Listing>(`/api/listings/${id}`, { method: "PUT", body: JSON.stringify(body) }),
    remove: (id: string, vendorId: string) => request<void>(`/api/listings/${id}?vendorId=${encodeURIComponent(vendorId)}`, { method: "DELETE" }),
  },

  purchases: {
    purchase: (body: { buyerId: string; listingId: string; quantity: number }) =>
      request<Purchase>("/api/purchases", { method: "POST", body: JSON.stringify(body) }),
    getForBuyer: (buyerId: string) => request<Purchase[]>(`/api/purchases/buyer/${buyerId}`),
  },
};
