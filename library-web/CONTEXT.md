# Library Web Context

Library Web is the browser interface for managing Books and their permanent
history. It lets people find Books, inspect current information, and safely
create or replace that information.

## Language

- **Book** is the current information held for a published work.
- **Author Name** is editable text inside a Book, not an independent entity.
- **Book Change** permanently records one field's old and new values.
- **Change Set** groups every Book Change from one creation or update.
- **Book History** is the ordered collection of complete Change Sets.

Use **Title**, **Short Description**, **Publish Date**, and **Author Names** in
code and user-facing copy. Do not introduce summary, release timestamp, audit
log, author profiles, or independent author management.
