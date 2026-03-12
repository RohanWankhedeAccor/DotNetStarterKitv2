namespace Domain.Enums;

/// <summary>
/// Represents the availability lifecycle of a product.
///
/// The valid state machine transitions are:
///   Draft → Active        (via Product.Activate)
///   Active → Discontinued (via Product.Discontinue)
///
/// Discontinued is a terminal state — once a product is discontinued it cannot be
/// reactivated. This invariant is enforced in the Application layer's command handler
/// rather than the entity itself, since the business rationale is operational rather
/// than a core domain invariant.
///
/// Values are explicitly numbered starting at 1 so that an uninitialized int (0)
/// is never silently treated as a valid product status.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// The product is being configured and is not yet available for selection
    /// in projects or orders. This is the initial state assigned by the Product constructor.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// The product is live and can be referenced in projects and orders.
    /// Pricing and description are authoritative in this state.
    /// </summary>
    Active = 2,

    /// <summary>
    /// The product has been permanently retired and is no longer available for new references.
    /// Existing references in historical records are preserved for audit accuracy.
    /// This is a terminal state.
    /// </summary>
    Discontinued = 3
}
