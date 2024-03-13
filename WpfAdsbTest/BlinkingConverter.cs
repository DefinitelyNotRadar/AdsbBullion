using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfAdsbTest
{
    public class BlinkingConverter : IValueConverter //IMultiValueConverter
    {

        // A dictionary to store the previous values of each cell
        private Dictionary<object, object> previousValues = new Dictionary<object, object>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //try
            //{
            //    // Get the current cell from the parameter
            //    //var cell = parameter as DataGridCell;

            //    //var cell = value[0] as DataGridCell;// there we get the row data 
            //    var cell = value[1] as DataGridCell;// there we get the cell data 

            //    // Check if the cell is null or not a data grid cell
            //    if (cell == null || !(cell.Column is DataGridBoundColumn))
            //    {
            //        return false;
            //    }

            //    // Get the binding path of the cell
            //    var bindingPath = (cell.Column as DataGridBoundColumn).Binding as Binding;

            //    //string a = bindingPath.Path.Path.ToString();
            //    // Check if the dictionary contains the binding path as a key
            //    if (previousValues.ContainsKey(bindingPath.Path.Path.ToString()))
            //    {
            //        // Get the previous value of the cell
            //        var previousValue = previousValues[bindingPath.Path.Path.ToString()];

            //        // Compare the current value with the previous value
            //        if (!Equals(value[1].ToString(), previousValue))
            //        {
            //            // Update the previous value in the dictionary
            //            previousValues[bindingPath.Path.Path.ToString()] = value[1].ToString();

            //            // Return true to indicate that the value has changed
            //            return true;
            //        }
            //        else
            //        {
            //            // Return false to indicate that the value has not changed
            //            return false;
            //        }
            //    }
            //    else
            //    {
            //        // Add the binding path and the current cell value to the dictionary
            //        previousValues.Add(bindingPath.Path.Path.ToString(), value[1].ToString());

            //        // Return false to indicate that this is the first time to see this value
            //        return false;
            //    }
            //}
            try
            {
                // Get the current cell from the parameter
                //var cell = parameter as DataGridCell;

                //var cell = value[0] as DataGridCell;// there we get the row data 
                var cell = value as DataGridCell;// there we get the cell data 

                // Check if the cell is null or not a data grid cell
                if (cell == null || !(cell.Column is DataGridBoundColumn))
                {
                    return "false";
                }

                // Get the binding path of the cell
                var bindingPath = (cell.Column as DataGridBoundColumn).Binding as Binding;

                //string a = bindingPath.Path.Path.ToString();
                // Check if the dictionary contains the binding path as a key
                if (previousValues.ContainsKey(bindingPath.Path.Path.ToString()))
                {
                    // Get the previous value of the cell
                    var previousValue = previousValues[bindingPath.Path.Path.ToString()];

                    // Compare the current value with the previous value
                    if (!Equals(value.ToString(), previousValue))
                    {
                        // Update the previous value in the dictionary
                        previousValues[bindingPath.Path.Path.ToString()] = value.ToString();

                        // Return true to indicate that the value has changed
                        return "true";
                    }
                    else
                    {
                        // Return false to indicate that the value has not changed
                        return "false";
                    }
                }
                else
                {
                    // Add the binding path and the current cell value to the dictionary
                    previousValues.Add(bindingPath.Path.Path.ToString(), value.ToString());

                    // Return false to indicate that this is the first time to see this value
                    return "false";
                }
            }
            catch (Exception ex)
            {
                return "false";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This method is not used in this example
            throw new NotImplementedException();
        }

      
    }
}

