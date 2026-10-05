global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.IO;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Text.Json;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Interop;
global using System.Windows.Media;
global using System.Windows.Threading;

// WPF/WinForms live in the same process (NotifyIcon uses WinForms).
// Keep ambiguous UI types explicitly bound to WPF.
global using WpfPoint = System.Windows.Point;
global using WpfDragEventArgs = System.Windows.DragEventArgs;
global using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
global using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
global using WpfDataObject = System.Windows.IDataObject;
