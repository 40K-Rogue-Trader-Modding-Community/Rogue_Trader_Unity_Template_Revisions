using System.Collections.Generic;

namespace Kingmaker.Editor.UIElements.ValuePicker
{
    public class ValuesContainer<T>
    {
        public List<T> RawValues { get; private set; }
        public bool FilterEnabled { get; private set; }
        public List<T> FilteredValues { get; private set; }

        public ValuesContainer(List<T> rawValues)
        {
            RawValues = rawValues;
            FilterEnabled = false;
        }

        public ValuesContainer(List<T> rawValues, IEnumerable<T> filteredValues)
        {
            RawValues = rawValues;
            FilterEnabled = true;
            FilteredValues = new List<T>(filteredValues);
        }
    }
}