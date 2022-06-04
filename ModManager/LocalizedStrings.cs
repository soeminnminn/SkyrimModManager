using System;
using System.Windows;

namespace ModManager
{
    internal static class LocalizedStrings
    {
        private static string FindResourceString(string resourceKey)
        {
            var str = Application.Current.FindResource(resourceKey) as string;
            return str != null ? str : string.Empty;
        }

        public static string AboutWindowTitleString 
        {
            get => FindResourceString("AboutWindowTitleString");
        }

        public static string MessageLoadingString
        {
            get => FindResourceString("MessageLoadingString");
        }

        public static string MessageReadyString
        {
            get => FindResourceString("MessageReadyString");
        }

        public static string MessageNeedSaveString
        {
            get => FindResourceString("MessageNeedSaveString");
        }

        public static string MessageSaveString
        {
            get => FindResourceString("MessageSaveString");
        }

        public static string MessageBackupString
        {
            get => FindResourceString("MessageBackupString");
        }

        public static string MessageRestoreString
        {
            get => FindResourceString("MessageRestoreString");
        }

        public static string MessageCantLoadString
        {
            get => FindResourceString("MessageCantLoadString");
        }

        public static string AboutString
        {
            get => FindResourceString("AboutString");
        }
    }
}
