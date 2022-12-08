// Copyright 2022 by Medallion Instrumentation Systems
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = FindReadOnlyByRef.Test.CSharpAnalyzerVerifier<
    FindReadOnlyByRef.FindReadOnlyByRefAnalyzer>;
using VerifyVB = FindReadOnlyByRef.Test.VisualBasicAnalyzerVerifier<
    FindReadOnlyByRef.FindReadOnlyByRefAnalyzer>;

namespace FindReadOnlyByRef.Test
{
    [TestClass]
    public class FindReadOnlyByRefUnitTest
    {
        /// <summary>
        /// The analyzer should accept a writable property passed by reference.
        /// </summary>
        [TestMethod]
        public async Task AcceptWritablePropertyByRef()
        {
            var test = @"
                Structure Point
                    Public Property X As Integer
                    Public Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByOne(ByRef x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        /// <summary>
        /// The analyzer should accept a ReadOnly property passed by value.
        /// </summary>
        [TestMethod]
        public async Task AcceptReadOnlyPropertyByVal()
        {
            var test = @"
                Structure Point
                    Public ReadOnly Property X As Integer
                    Public ReadOnly Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByOne(x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        /// <summary>
        /// The analyzer should reject a ReadOnly property passed by reference.
        /// </summary>
        [TestMethod]
        public async Task RejectReadOnlyPropertyByRef()
        {
            var test = @"
                Structure Point
                    Public ReadOnly Property X As Integer
                    Public ReadOnly Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByOne(ByRef x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("property", "point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }

        /// <summary>
        /// The analyzer should reject a property with no setter passed by reference.
        /// </summary>
        [TestMethod]
        public async Task RejectNoSetterPropertyByRef()
        {
            var test = @"
                Structure Point
                    Public ReadOnly Property X As Integer
                        Get
                            Return 0
                        End Get
                    End Property

                    Public ReadOnly Property Y As Integer
                        Get
                            Return 0
                        End Get
                    End Property
                End Structure

                Class Program
                    Sub IncreaseByOne(ByRef x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("property", "point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }

        /// <summary>
        /// The analyzer should accept a writable field passed by reference.
        /// </summary>
        [TestMethod]
        public async Task AcceptWritableFieldByRef()
        {
            var test = @"
                Structure Point
                    Public X As Integer
                    Public Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByOne(ByRef x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        /// <summary>
        /// The analyzer should reject a ReadOnly field passed by reference.
        /// </summary>
        [TestMethod]
        public async Task RejectReadOnlyFieldByRef()
        {
            var test = @"
                Structure Point
                    Public ReadOnly X As Integer
                    Public ReadOnly Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByOne(ByRef x As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByOne({|#0:point.X|})
                    End Sub
                End Class";

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("field", "point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }

        /// <summary>
        /// The analyzer should accept a writable property passed by reference
        /// as an out-of-order keyword argument.
        /// </summary>
        [TestMethod]
        public async Task AcceptWritablePropertyByRefKeyword()
        {
            var test = @"
                Structure Point
                    Public Property X As Integer
                    Public Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByAmount(ByRef x As Integer, amount As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByAmount(amount:=5, x:={|#0:point.X|})
                    End Sub
                End Class";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        /// <summary>
        /// The analyzer should accept a ReadOnly property passed by value as
        /// an out-of-order keyword argument.
        /// </summary>
        [TestMethod]
        public async Task AcceptReadOnlyPropertyByValKeyword()
        {
            var test = @"
                Structure Point
                    Public ReadOnly Property X As Integer
                    Public ReadOnly Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByAmount(ByRef x As Integer, amount As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        Dim temp As Integer = 0
                        IncreaseByAmount(amount:={|#0:point.X|}, x:=temp)
                    End Sub
                End Class";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        /// <summary>
        /// The analyzer should reject a ReadOnly property passed by reference
        /// as an out-of-order keyword argument.
        /// </summary>
        [TestMethod]
        public async Task RejectReadOnlyPropertyByRefKeyword()
        {
            var test = @"
                Structure Point
                    Public ReadOnly Property X As Integer
                    Public ReadOnly Property Y As Integer
                End Structure

                Class Program
                    Sub IncreaseByAmount(ByRef x As Integer, amount As Integer)
                        x = x + 1
                    End Sub

                    Sub Main()
                        Dim point As New Point
                        IncreaseByAmount(amount:=5, x:={|#0:point.X|})
                    End Sub
                End Class";

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("property", "point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }
    }
}
