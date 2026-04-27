using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Threading;
using System.Globalization;


namespace Командное_управление_проектами
{
    public partial class App : Application
    {
        public App()
        {
            // Устанавливаем русскую культуру для всего приложения
            var cultureInfo = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Student");

            // Это гарантирует, что все элементы WPF будут использовать эту культуру для форматирования
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag))

            );
        }
    }
}
