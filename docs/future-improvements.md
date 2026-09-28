# Future Improvements

This document tracks architectural improvements for the MDV implementation. The current direction is viable, but the
items below should be addressed before the library’s public contracts become difficult to change.

## MDV principles to preserve

These principles are intentional and should remain part of the architecture:

- `D` is view-shaped data, not the domain model.
- The control tree and data graph are separate graphs.
- Controllers may reference data, but data must not reference controls or rendering objects.
- Generated data types should be strongly typed, AOT-friendly, and trimming-safe.
- Domain models should map to and from view data outside the control implementation.
- Generic controls may use strongly typed data collections and item factories.

The generated controller and generated data type do not need to be completely independent. Their relationship is an
intentional part of MDV. The important boundary is that the data object remains independent of the control tree.

## Priority 1: Collection synchronization contract

Collection behavior is currently delegated to handwritten controls. This should become a defined and reusable framework
contract rather than a separate implementation in every items control.

Define behavior for:

- Adding an item.
- Removing an item.
- Replacing an item.
- Moving an item.
- Resetting the collection.
- Replacing the collection instance.
- Changing a property on an existing item.
- Preserving or clearing selection after collection changes.
- Updating collections while a control is rendering.
- Batching multiple collection changes into one view update.

`ObservableCollection<T>` handles collection-level notifications, but it does not automatically propagate item-level
property changes. The framework should explicitly define whether item data implements `INotifyPropertyChanged` and how
item controllers respond to those changes.

Add integration tests for every supported collection operation before standardizing the public API.

### Collection update strategies

The framework should support more than one update strategy. The simplest strategy is reset and rebuild: when a new
collection is received, discard the existing item controllers and recreate the visible items. This does not require
item identity and is reasonable for small collections or controls such as the current `ComboBox`, whose drop-down is
created from the current collection each time it opens. The current implementation is therefore effectively doing a
full replacement of the drop-down, but it does not synchronize an already-open drop-down or the selected-item view.

Another strategy is to replace the collection and reconcile the old and new sequences. A diff can identify insertions,
removals, replacements, and moves, allowing a list control to update its existing item controllers instead of
rebuilding the entire list. This is especially useful for a visible `ListBox` and may also be useful for tree nodes.
The application should be able to replace a collection populated from server JSON without manually issuing an
`Insert` or `Remove` for every change. Without stable item keys, the reconciler can use positional comparison or fall
back to a reset; stable identity is needed when per-item controller state must follow a logical item through a move.

The reusable synchronizer described as option #4 is a framework-level mechanism for both paths, not necessarily one
specific diff algorithm. It should normalize collection notifications and collection replacement into operations such
as add, remove, replace, move, and reset. Controls would retain their own visual policies: a `ComboBox` may rebuild its
drop-down, a `ListBox` may apply incremental changes, and a tree control may maintain a synchronizer for each node's
child collection. Item-controller creation and ownership can be a separate layer so that the basic collection watcher
does not prematurely define templates, virtualization, or rendering behavior.

Selection must be an explicit part of the contract. Preserving only an integer selected index is incorrect when an item
is inserted before the selection. A control may preserve the selected item's stable identity and calculate its new
index, or use a documented positional policy when identity is unavailable. If the selected item is removed, the
contract should define whether selection moves to the next item, the previous item, or becomes empty. The framework
should not require a single policy for every control, but it should provide the collection changes needed for each
control to implement its policy consistently.

The preferred direction remains undecided between reset/rebuild and replacement with diffing. Before standardizing a
public API, compare both approaches with integration tests covering server refreshes, insertion before the selected
item, removal of the selected item, reordering, item property changes, collection replacement, and updates while a
control is visible. A full rebuild is a valid fallback when identity or an efficient diff is unavailable.

## Priority 2: Item controller and template creation

The current data-controller registry maps an exact concrete data `Type` to a controller factory. This is convenient for
generated controls, but it should not be the only public abstraction.

Exact runtime-type lookup has several limitations:

- Derived data types do not match a registration for their base type.
- Proxy and decorator data types do not match automatically.
- Applications cannot easily select an alternate renderer for the same data type.
- The global registry can make tests and multiple application instances interfere with one another.

Introduce a control-facing item factory or template abstraction, conceptually:

```csharp
Func<TItem, Controllable> ItemFactory
```

The generated registry can supply the default factory, while application code can replace or override it. The desired
relationship is:

```text
ObservableCollection<TItemData>
		-> item factory or template
		-> item controller
		-> item view
```

The data object should remain unaware of the controller that renders it.

## Priority 3: DataContext lifecycle and ownership

Define the lifecycle contract for assigning and replacing `DataContext` objects.

Document and test:

- Whether replacing `DataContext` after initialization is supported.
- Unsubscription from the previous data object.
- Ownership of data objects by application code versus controllers.
- Sharing one data object between multiple controls.
- Sharing child data objects between multiple parent data objects.
- Reusing data objects after a control is detached.
- Cleanup of collection and item subscriptions during detach.
- Behavior when a controller is attached, detached, and attached again.

A useful ownership rule is:

> A controller does not own the lifetime of `D`; it owns only its subscriptions to `D`.

All generated and handwritten subscription paths should follow the same lifecycle rules.

## Priority 4: UI-thread and batching policy

`INotifyPropertyChanged` does not define which thread may modify data. The library should establish a policy before controls
are widely used with asynchronous operations, timers, network callbacks, or WebAssembly interop.

Decide whether:

- Data must be changed on the UI thread.
- Changes are automatically dispatched to the UI thread.
- Off-thread changes are rejected or reported.
- Multiple changes may be batched into one layout and render invalidation.
- `PropertyChanged(null)` and `PropertyChanged("")` mean refresh-all.
- Collection notifications follow the same dispatch and batching rules.

The current generated code treats a null or empty property name as a full synchronization request. If that behavior is
kept, it should be documented and tested as part of the data contract.

## Priority 5: Domain model and view data boundary

Generated data classes should remain view-specific. They should not become the application’s domain model by convention.

For example:

```text
Customer          domain model
CustomerListData  view-shaped data for a list view
CustomerItemData  view-shaped data for an item renderer
```

The library should document two-way mapping where necessary:

```text
Customer -> CustomerItemData
CustomerItemData changes -> Customer
```

Partial data classes are useful for small mappings. Larger applications may need explicit adapters or presenters so that
view changes do not become domain-model changes.

## Priority 6: Registration scope

Control, data-controller, and theme registration is currently process-global. This is simple for generated startup code,
but it creates future problems for:

- Multiple application instances in one process.
- Unit tests with different registrations.
- Hot reload.
- Plugin loading and unloading.
- Registration order.
- Conflicting control versions.

Consider introducing an application or runtime registration context, such as a `GuiRuntime` or `AppContext`. Static
registration may remain as a convenience layer, but the underlying APIs should eventually support scoped registrations.

## Priority 7: Generated contract diagnostics

The generator should reject invalid relationships as early as possible and produce clear diagnostics.

Important validation areas include:

- Forwarded bindings referring to missing named controls.
- Forwarded bindings referring to missing data properties.
- Incompatible generic item types and item controllers.
- Unsupported collection binding modes.
- Invalid nullability combinations.
- Data/controller namespace mismatches.
- Duplicate or ambiguous generated registrations.
- Data classes whose generated and handwritten declarations disagree.

Runtime string-based failures should be minimized when the generator can validate the same relationship at compile time.

## Priority 8: Template and content-host contract

The current loader explicitly separates control-owned template content from content supplied by a parent:

```text
LoadTemplateContent
    -> builds the control's internal ZUI-defined structure

LoadParentContent
    -> adds parent-supplied content to ContentHost
```

This is similar in concept to template and content-presenter systems in other GUI frameworks. The current API is a
good minimal contract for generated controls, but it should be formalized before more template-aware controls are added.

Document and test:

- The ordering of `LoadTemplateContent` and `LoadParentContent`.
- The ownership distinction between template content and parent content.
- The default behavior of both lifecycle methods.
- The role of `ContentHost` as the destination for parent content.
- Whether parent content can be replaced after initial construction.
- Whether content hosts remain valid when a control is detached and reattached.
- Whether content changes require layout, rendering, or both to be invalidated.

The current `ContentHost` represents one content destination. Future controls may need multiple named content slots,
such as:

```text
Content       -> main content host
Header        -> header host
Footer        -> footer host
Overlay       -> overlay host
Items         -> items host
```

A future template system could declare these slots and allow parent content to target a named slot. This would be a
natural extension of `ContentHost`, rather than requiring each control to add another specialized loading method.

Possible future abstractions include:

- Named content hosts or presenters.
- A template contract that declares available content slots.
- Explicit content replacement and removal operations.
- A presenter-like object that maps content data to a host view.
- Template diagnostics for missing, duplicate, or incompatible content slots.

`ScrollViewer` is the current motivating example: its internal template creates the viewport, content window, and
scrollbars, while parent content is directed to the content window. A future multi-slot system could express that
relationship declaratively as `Content -> _contentWindow`.

## Priority 9: Generated data API stability

Decide which parts of generated data classes are public compatibility contracts:

- Class names.
- Property names.
- Property types.
- Constructor signatures.
- Nullability annotations.
- Collection types.
- Notification semantics.
- Generic constraints.

Once applications consume generated data types directly, changing these details becomes a source and binary compatibility
concern. The generator should have compatibility tests for representative generated controls and generic controls.

## Priority 10: JSON initialization versus generated initialization

The current loader applies JSON data properties at runtime using property names and runtime type information. This is
convenient, but it is less aligned with the compile-time MDV goal than generated typed initialization.

A future design could generate typed initialization for declared data literals and use JSON primarily for view structure,
styles, and literals that genuinely need runtime interpretation.

This is lower priority than collection and lifecycle contracts, but it should be considered before the JSON data-property
behavior becomes a permanent public API.

## Suggested implementation order

1. Specify and test collection synchronization.
2. Specify data and controller subscription lifetimes.
3. Specify UI-thread, batching, and full-refresh behavior.
4. Add a reusable item collection synchronization mechanism.
5. Add an overridable item factory or template abstraction.
6. Add generator diagnostics for invalid bindings and generic relationships.
7. Introduce scoped registration behind the existing generated initialization path.
8. Formalize template and content-host contracts, including named content slots.
9. Define generated data API compatibility rules.
10. Revisit runtime JSON data initialization.

## Explicit non-goal

Do not remove all coupling between a generated controller and its generated data type. A view-specific controller consuming a
view-specific data shape is the intended MDV relationship. The required decoupling is between data and controls, not between
the view controller and the data contract it renders.
