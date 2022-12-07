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
    }
}
