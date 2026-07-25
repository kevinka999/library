# Library

This context maintains books and exposes their recorded history of changes.

## Language

**Book**:
The current information held for a published work, including its title, short description, publication date, and a non-empty unordered set of author names.

**Title**:
The required nonblank name of a Book, containing no more than 300 characters.

**Short Description**:
Required nonblank text describing a Book in no more than 1,000 characters.
_Avoid_: Summary, synopsis

**Publish Date**:
The required calendar date on which a Book is or is expected to be published; it has no time-of-day component and may be in the future.
_Avoid_: Published at, release timestamp

**Author Name**:
Editable nonblank text of no more than 200 characters identifying an author associated with a Book; it has no independent identity, lifecycle, or role in this context.
_Avoid_: Author entity, author profile, co-author

**Book Change**:
A permanent, immutable record of the before and after values of one Book field, created when the field is initially set or later modified.
_Avoid_: Audit record, history entry

**Change Set**:
The complete collection of Book Changes produced by one creation or modification, presented together as one history item.
_Avoid_: Batch, audit transaction

**Changed Field**:
The Book field whose previous and new values are recorded by a Book Change: title, short description, publication date, or author names.
_Avoid_: Change type, operation type

**Book History**:
The permanently retained, ordered collection of complete Change Sets associated with a Book.
_Avoid_: Audit log
