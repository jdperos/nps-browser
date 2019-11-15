using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ReactiveUI;

namespace NPS.Helpers.CustomComponents
{
    public class CheckBoxComboBox : UserControl
    {
        private readonly List<string> _items = new List<string>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private readonly ObservableCollection<CheckBox> _checkBoxes
            = new ObservableCollection<CheckBox>();

        private readonly CheckBoxComboBoxItems _itemsWrapper;
        private readonly CheckBoxComboBoxCheckBoxes _checkBoxesWrapper;
        private readonly ItemsControl _itemsControl;
        private readonly Popup _popup;
        private readonly ToggleButton _button;
        private readonly TextBlock _text;

        public IList<string> Items => _itemsWrapper;
        public IReadOnlyList<CheckBox> CheckBoxItems => _checkBoxesWrapper;

        public event EventHandler CheckBoxCheckedChanged;

        public CheckBoxComboBox()
        {
            _itemsWrapper = new CheckBoxComboBoxItems(this);
            _checkBoxesWrapper = new CheckBoxComboBoxCheckBoxes(this);

            InitializeComponent();

            _itemsControl = this.FindControl<ItemsControl>("Items");
            _itemsControl.Items = _checkBoxes;

            _popup = this.FindControl<Popup>("Popup");
            _button = this.FindControl<ToggleButton>("Button");
            _text = this.FindControl<TextBlock>("Content");

            this.WhenAnyValue(x => x._button.IsChecked)
                .Subscribe(n =>
                {
                    if (n == true)
                    {
                        _popup.Open();
                    }
                    else
                    {
                        _popup.Close();
                    }
                });

            _popup.Closed += (sender, args) => _button.IsChecked = false;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (!e.Handled)
            {
                if (_popup?.IsInsidePopup((IVisual) e.Source) == false)
                {
                    _button.IsChecked = !_button.IsChecked;
                    e.Handled = true;
                }
            }

            base.OnPointerPressed(e);
        }

        private void UpdateText()
        {
            _text.Text = string.Join(", ", _checkBoxes.Where(x => x.IsChecked == true).Select(x => x.Content));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CheckedChanged()
        {
            UpdateText();
            CheckBoxCheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        private sealed class CheckBoxComboBoxItems : IList<string>
        {
            private readonly CheckBoxComboBox _owner;

            public CheckBoxComboBoxItems(CheckBoxComboBox owner)
            {
                _owner = owner;
            }

            public IEnumerator<string> GetEnumerator()
            {
                return _owner._items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(string item)
            {
                _owner._items.Add(item);
                var checkBox = new CheckBox
                {
                    Content = item
                };

                var d = checkBox.WhenAnyValue(x => x.IsChecked)
                    .Subscribe(_ => _owner.CheckedChanged());

                _owner._checkBoxes.Add(checkBox);
                _owner._disposables.Add(d);
            }

            public void Clear()
            {
                foreach (var disposable in _owner._disposables)
                {
                    disposable.Dispose();
                }

                _owner._items.Clear();
                _owner._checkBoxes.Clear();
                _owner._disposables.Clear();
            }

            public bool Contains(string item)
            {
                return _owner._items.Contains(item);
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                _owner._items.CopyTo(array, arrayIndex);
            }

            public bool Remove(string item)
            {
                var index = _owner._items.IndexOf(item);
                if (index < 0)
                {
                    return false;
                }

                _owner._disposables[index].Dispose();

                _owner._items.RemoveAt(index);
                _owner._checkBoxes.RemoveAt(index);
                _owner._disposables.RemoveAt(index);
                return true;
            }

            public int Count => _owner._items.Count;
            bool ICollection<string>.IsReadOnly => false;

            public int IndexOf(string item)
            {
                return _owner._items.IndexOf(item);
            }

            public void Insert(int index, string item)
            {
                _owner._items.Insert(index, item);

                var checkBox = new CheckBox
                {
                    Content = item
                };

                var d = checkBox.WhenAnyValue(x => x.IsChecked)
                    .Subscribe(_ => _owner.CheckedChanged());

                _owner._checkBoxes.Insert(index, checkBox);
                _owner._disposables.Insert(index, d);
            }

            public void RemoveAt(int index)
            {
                _owner._disposables[index].Dispose();

                _owner._items.RemoveAt(index);
                _owner._checkBoxes.RemoveAt(index);
                _owner._disposables.RemoveAt(index);
            }

            public string this[int index]
            {
                get => _owner._items[index];
                set
                {
                    _owner._items[index] = value;
                    _owner._checkBoxes[index].Content = value;
                }
            }
        }

        private class CheckBoxComboBoxCheckBoxes : IReadOnlyList<CheckBox>
        {
            private readonly CheckBoxComboBox _owner;

            public CheckBoxComboBoxCheckBoxes(CheckBoxComboBox owner)
            {
                _owner = owner;
            }

            public IEnumerator<CheckBox> GetEnumerator()
            {
                return _owner._checkBoxes.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => _owner._checkBoxes.Count;

            public CheckBox this[int index] => _owner._checkBoxes[index];
        }
    }
}