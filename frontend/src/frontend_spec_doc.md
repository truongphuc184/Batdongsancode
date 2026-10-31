# Frontend Specification
```json
[
  {
    "id": "listingDetail",
    "title": "Property Details",
    "components": [
      { "type": "imageCarousel", "source": "listing.imageUrls" },
      { "type": "text", "field": "title" },
      { "type": "price", "field": "price" },
      { "type": "area", "field": "area" },
      { "type": "address", "field": "address" },
      { "type": "description", "field": "description" },
      { "type": "button", "label": "Contact Owner", "action": "POST /api/contacts (placeholder)" },
      { "type": "button", "label": "Save to Favorites", "action": "POST /api/favorites" }
    ]
  },
  {
    "id": "createListing",
    "title": "Create New Listing",
    "authRequired": true,
    "allowedRoles": ["Seller", "Broker", "Agency", "Admin"],
    "components": [
      { "type": "input", "name": "title", "label": "Title" },
      { "type": "textarea", "name": "description", "label": "Description" },
      { "type": "input", "name": "price", "label": "Price", "inputType": "number" },
      { "type": "input", "name": "area", "label": "Area (m²)", "inputType": "number" },
      { "type": "input", "name": "address", "label": "Address" },
      { "type": "select", "name": "type", "label": "Type", "options": ["Sale", "Rent"] },
      { "type": "fileUpload", "name": "images", "label": "Images / Videos", "multiple": true },
      { "type": "button", "label": "Publish", "action": "POST /api/listings" }
    ]
  },
  {
    "id": "profile",
    "title": "User Profile",
    "authRequired": true,
    "components": [
      { "type": "text", "field": "email", "editable": false },
      { "type": "input", "name": "fullName", "label": "Full Name" },
      { "type": "select", "name": "role", "label": "Role", "options": ["Buyer", "Seller", "Broker", "Agency", "Admin"], "editable": false },
      { "type": "button", "label": "Update Profile", "action": "PUT /api/users/me" }
    ]
  }
]
```