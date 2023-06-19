// -----------------------------------------------------------------------
// <copyright file="ProgressBarWidthConverter.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Globalization;
using System.Windows.Data;

namespace BsaBrowser.Controls
{
    /// <summary>
    /// </summary>
    /// <seealso cref="System.Windows.Data.IMultiValueConverter" />
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        /// <summary>
        ///     Converts the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var contentWidth = (double)values[0];
            var parentMinWidth = (double)values[1];

            return Math.Max(contentWidth, parentMinWidth);
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetTypes">The target types.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}