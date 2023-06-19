using System;
using System.Collections.Generic;
using System.Windows;

namespace BsaBrowser.Controls
{
    /// <summary>
    /// Indicates where an item should be dropped.
    /// </summary>
    public enum DropPosition
    {
        /// <summary>
        /// Add the item to the target item.
        /// </summary>
        Add,

        /// <summary>
        /// Insert the item before the target item.
        /// </summary>
        InsertBefore,

        /// <summary>
        /// Insert the item after the target item.
        /// </summary>
        InsertAfter
    }

    /// <summary>
    /// Allows an object to be dragged.
    /// </summary>
    public interface IDragSource
    {
        /// <summary>
        /// Gets a value indicating whether this instance is possible to drag.
        /// </summary>
        bool IsDraggable { get; }

        /// <summary>
        /// Detaches this instance (for move and drop somewhere else).
        /// </summary>
        void Detach();
    }

    /// <summary>
    /// Specifies the effects of a drag-and-drop operation.
    /// </summary>
    public enum DragDropEffect
    {
        /// <summary>
        /// The drop target does not accept the data.
        /// </summary>
        None = 0,

        /// <summary>
        /// The data is copied to the drop target.
        /// </summary>
        Copy = 1,

        /// <summary>
        /// The data from the drag source is moved to the drop target.
        /// </summary>
        Move = 2,

        /// <summary>
        /// The data from the drag source is linked to the drop target.
        /// </summary>
        Link = 4
    }

    /// <summary>
    /// Allows an object to receive dropped items.
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>
        /// Determines whether the specified <paramref name="node" /> can be dropped here.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="dropPosition">The position where the <paramref name="node" /> should be dropped.</param>
        /// <param name="effect">The drag/drop effect.</param>
        /// <returns><c>true</c> if this instance accepts a drop of the specified <paramref name="node" />; otherwise, <c>false</c>.</returns>
        bool CanDrop(IDragSource node, DropPosition dropPosition, DragDropEffect effect);

        /// <summary>
        /// Drops the specified <paramref name="items" /> at this object.
        /// </summary>
        /// <param name="items">The items to drop.</param>
        /// <param name="dropPosition">The position where the <paramref name="items" /> should be dropped.</param>
        /// <param name="effect">The drag/drop effect.</param>
        /// <param name="initialKeyStates">The initial drag/drop key states.</param>
        void Drop(IEnumerable<IDragSource> items, DropPosition dropPosition, DragDropEffect effect, DragDropKeyStates initialKeyStates);
    }
}
