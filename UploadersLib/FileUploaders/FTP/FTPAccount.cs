﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (C) 2007-2014 ShareX Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using HelpersLib;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using UploadersLib.HelperClasses;

namespace UploadersLib
{
    public class FTPAccount : ICloneable
    {
        [Category("Account"), Description("Connection protocol"), DefaultValue(FTPProtocol.FTP)]
        public FTPProtocol Protocol { get; set; }

        [Category("FTP"), Description("Shown in the list as: Name - Server:Port")]
        public string Name { get; set; }

        [Category("FTP"), Description("Host, e.g. google.com")]
        public string Host { get; set; }

        [Category("FTP"), Description("Port number"), DefaultValue(21)]
        public int Port { get; set; }

        [Category("FTP")]
        public string Username { get; set; }

        [Category("FTP"), PasswordPropertyText(true)]
        public string Password { get; set; }

        [Category("FTP"), Description("Choose an appropriate protocol to be accessed by the server. This affects the server address."), DefaultValue(ServerProtocol.ftp)]
        public ServerProtocol ServerProtocol { get; set; }

        [Category("FTP"), Description("FTP sub folder path, example: Screenshots.\r\nYou can use name parsing: %y = year, %mo = month.")]
        public string SubFolderPath { get; set; }

        [Category("FTP"), Description("Choose an appropriate protocol to be accessed by the browser"), DefaultValue(BrowserProtocol.http)]
        public BrowserProtocol BrowserProtocol { get; set; }

        [Category("FTP"), Description("URL = HttpHomePath + SubFolderPath + FileName\r\nIf HttpHomePath is empty then URL = Host + SubFolderPath + FileName\r\n%host = Host")]
        public string HttpHomePath { get; set; }

        [Category("FTP"), Description("Automatically add sub folder path to end of http home path"), DefaultValue(true)]
        public bool HttpHomePathAutoAddSubFolderPath { get; set; }

        [Category("FTP"), Description("Don't add file extension to URL"), DefaultValue(false)]
        public bool HttpHomePathNoExtension { get; set; }

        [Category("FTP"), Description("Set true for active or false for passive"), DefaultValue(false)]
        public bool IsActive { get; set; }

        [Category("FTP"), Description("ftp://Host:Port"), Browsable(false)]
        public string FTPAddress
        {
            get
            {
                if (string.IsNullOrEmpty(Host))
                {
                    return string.Empty;
                }

                return string.Format("{0}{1}:{2}", ServerProtocol.GetDescription(), Host, Port);
            }
        }

        private string exampleFilename = "screenshot.jpg";

        [Category("FTP"), Description("Preview of the FTP path based on the settings above")]
        public string PreviewFtpPath
        {
            get
            {
                return GetFtpPath(exampleFilename);
            }
        }

        [Category("FTP"), Description("Preview of the HTTP path based on the settings above")]
        public string PreviewHttpPath
        {
            get
            {
                return GetUriPath(exampleFilename);
            }
        }

        [Category("FTPS"), Description("Type of SSL to use. Explicit is TLS, Implicit is SSL."), DefaultValue(FTPSEncryption.Explicit)]
        public FTPSEncryption FTPSEncryption { get; set; }

        [Category("FTPS"), Description("Certificate file location. Optional setting.")]
        [Editor(typeof(CertFileNameEditor), typeof(UITypeEditor))]
        public string FTPSCertificateLocation { get; set; }

        [Category("SFTP"), Description("OpenSSH key passphrase"), PasswordPropertyText(true)]
        public string Passphrase { get; set; }

        [Category("SFTP"), Description("Key location")]
        [Editor(typeof(KeyFileNameEditor), typeof(UITypeEditor))]
        public string Keypath { get; set; }

        public FTPAccount()
        {
            Protocol = FTPProtocol.FTP;
            Name = "New account";
            Host = "host";
            Port = 21;
            ServerProtocol = ServerProtocol.ftp;
            SubFolderPath = string.Empty;
            BrowserProtocol = BrowserProtocol.http;
            HttpHomePath = string.Empty;
            HttpHomePathAutoAddSubFolderPath = true;
            HttpHomePathNoExtension = false;
            IsActive = false;
            FTPSEncryption = FTPSEncryption.Explicit;
            FTPSCertificateLocation = string.Empty;
        }

        public string GetSubFolderPath(string filename = null)
        {
            string path = NameParser.Parse(NameParserType.URL, SubFolderPath.Replace("%host", Host));
            return URLHelpers.CombineURL(path, filename);
        }

        public string GetHttpHomePath()
        {
            // @ deprecated
            if (HttpHomePath.StartsWith("@"))
            {
                HttpHomePath = HttpHomePath.Substring(1);
                HttpHomePathAutoAddSubFolderPath = false;
            }

            HttpHomePath = URLHelpers.RemovePrefixes(HttpHomePath);

            return NameParser.Parse(NameParserType.URL, HttpHomePath.Replace("%host", Host));
        }

        public string GetUriPath(string filename, string subFolderPath = null)
        {
            if (string.IsNullOrEmpty(Host))
            {
                return string.Empty;
            }

            if (HttpHomePathNoExtension)
            {
                filename = Path.GetFileNameWithoutExtension(filename);
            }

            filename = URLHelpers.URLEncode(filename);

            if (subFolderPath == null)
            {
                subFolderPath = GetSubFolderPath();
            }

            subFolderPath = URLHelpers.URLPathEncode(subFolderPath);

            string httpHomePath = GetHttpHomePath();
            httpHomePath = URLHelpers.URLPathEncode(httpHomePath);

            string path;

            if (string.IsNullOrEmpty(httpHomePath))
            {
                string host = Host;

                if (host.StartsWith("ftp."))
                {
                    host = host.Substring(4);
                }

                path = URLHelpers.CombineURL(host, subFolderPath, filename);
            }
            else
            {
                if (HttpHomePathAutoAddSubFolderPath)
                {
                    path = URLHelpers.CombineURL(httpHomePath, subFolderPath);
                }
                else
                {
                    path = httpHomePath;
                }

                if (path.EndsWith("="))
                {
                    path += filename;
                }
                else
                {
                    path = URLHelpers.CombineURL(path, filename);
                }
            }

            string browserProtocol = BrowserProtocol.GetDescription();

            if (!path.StartsWith(browserProtocol))
            {
                path = browserProtocol + path;
            }

            return path;
        }

        public string GetFtpPath(string filemame)
        {
            if (string.IsNullOrEmpty(FTPAddress))
            {
                return string.Empty;
            }

            return URLHelpers.CombineURL(FTPAddress, GetSubFolderPath(filemame));
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}:{2}", Name, Host, Port);
        }

        public FTPAccount Clone()
        {
            return MemberwiseClone() as FTPAccount;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}