---
status: superseded by ADR-0003
---

# Store author names as JSON

Store each Book's collection of author names as JSON in the `Books` table rather than introducing author or book-author tables. Author names have no independent identity or lifecycle in this service, and the simpler write model outweighs the reduced ability to index author-specific searches; normalization can be reconsidered if the catalog or author-query requirements grow substantially.
