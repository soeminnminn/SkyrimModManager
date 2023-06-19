using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BsaBrowser.Commons;

namespace BsaBrowser.Converters
{
    public class FileIconConverter : IValueConverter
    {
        private ImageSource GetIconFromResource(Archive.ArchiveNode val)
        {
            string resourcePath = "Resources/FolderOpen.png";

            if (val.Entry != null)
            {
                var fileExtension = val.Entry.extension;
                if (string.IsNullOrEmpty(fileExtension))
                    resourcePath = "Resources/File.png";
                else
                {
                    string extension = fileExtension.ToLower().TrimStart('.');
                    if ((extension == "avi") || (extension == "bik") || (extension == "swf"))
                        resourcePath = "Resources/VideoFile.png";
                    else if ((extension == "wav") || (extension == "mp3") || (extension == "fuz") || (extension == "xwm"))
                        resourcePath = "Resources/MusicFile.png";
                    else if (extension == "xml")
                        resourcePath = "Resources/XMLFile.png";
                    else if ((extension == "dds") || (extension == "tga") || (extension == "png") || (extension == "jpg"))
                        resourcePath = "Resources/PictureFile.png";
                    else if (extension == "nif")
                        resourcePath = "Resources/NifFile.png";
                    else if ((extension == "txt") || (extension == "log"))
                        resourcePath = "Resources/TextFile.png";
                    else if ((extension == "ini") || (extension == "inf") || (extension == "lod"))
                        resourcePath = "Resources/ConfigFile.png";
                    else if (extension == "pex")
                        resourcePath = "Resources/ScriptFile.png";
                    else if ((extension == "psc") || (extension == "seq"))
                        resourcePath = "Resources/CodeFile.png";
                    else if ((extension == "bsa") || (extension == "ba2"))
                        resourcePath = "Resources/ArchiveFile.png";
                    else if (extension == "tri")
                        resourcePath = "Resources/ObjectFile.png";
                    else
                        resourcePath = "Resources/File.png";
                }
            }


            try
            {
                return new BitmapImage(new Uri($"pack://application:,,,/{resourcePath}"));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return null;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            ImageSource image = null;

            if (parameter is string pathParam && !string.IsNullOrEmpty(pathParam))
            {
                try
                {
                    return new BitmapImage(new Uri($"pack://application:,,,/{pathParam}"));
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            else if (value is Archive.ArchiveNode val)
            {
                try
                {
                    if (val.IsFolder)
                    {
                        image = val.IsExpanded ? SystemIcons.FolderSmallOpen.ToImageSource() : SystemIcons.FolderSmall.ToImageSource();
                    }
                    else
                    {
                        image = SystemIcons.GetFileIcon(val.Path).ToImageSource();
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }

                if (image == null)
                {
                    image = GetIconFromResource(val);
                }
            }

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
