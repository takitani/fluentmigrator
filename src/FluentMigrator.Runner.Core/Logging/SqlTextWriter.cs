﻿#region License
// Copyright (c) 2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System.IO;
using System.Text;

namespace FluentMigrator.Runner.Logging
{
    /// <summary>
    /// A <see cref="TextWriter"/> implementation that puts everything into multi-line comments
    /// </summary>
    internal class SqlTextWriter : TextWriter
    {
        private readonly TextWriter _innerWriter;
        private bool _commentStarted = false;
        private bool _hadEmptyLine = true;

        public SqlTextWriter(TextWriter innerWriter)
        {
            _innerWriter = innerWriter;
        }

        /// <inheritdoc />
        public override Encoding Encoding => _innerWriter.Encoding;

        /// <inheritdoc />
        public override void WriteLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!_commentStarted)
                {
                    if (!_hadEmptyLine)
                    {
                        _innerWriter.WriteLine();
                        _hadEmptyLine = true;
                    }

                    _innerWriter.WriteLine("/**");
                }

                _innerWriter.WriteLine($" * {value}");
            }
            else if (_commentStarted)
            {
                _innerWriter.WriteLine(" *");
            }
            else if (!_hadEmptyLine)
            {
                _innerWriter.WriteLine();
                _hadEmptyLine = true;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseComment(false);
            }
        }

        public void WriteLineDirect(string message)
        {
            if (_commentStarted)
            {
                CloseComment();
            }

            _innerWriter.WriteLine(message);
        }

        private void CloseComment(bool addEmptyLine = true)
        {
            if (!_commentStarted)
                return;

            _innerWriter.WriteLine(" */");

            if (addEmptyLine)
            {
                _innerWriter.WriteLine();
            }

            _hadEmptyLine = addEmptyLine;
        }
    }
}
