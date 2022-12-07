using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = FindReadOnlyByRef.Test.CSharpCodeFixVerifier<
    FindReadOnlyByRef.FindReadOnlyByRefAnalyzer,
    FindReadOnlyByRef.FindReadOnlyByRefCodeFixProvider>;
using VerifyVB = FindReadOnlyByRef.Test.VisualBasicCodeFixVerifier<
    FindReadOnlyByRef.FindReadOnlyByRefAnalyzer,
    FindReadOnlyByRef.FindReadOnlyByRefCodeFixProvider>;

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

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }

        /// <summary>
        /// The analyzer should reject a Private Set property passed by reference.
        /// </summary>
        /// <remarks>
        /// Ideally, the analyzer should be smart enough to detect whether or
        /// not the accessibility of a property allows it to be written to in
        /// the exact context it's actually used in. I don't know how easy that
        /// would be to implement.
        /// </remarks>
        [TestMethod]
        public async Task RejectPrivateSetPropertyByRef()
        {
            var test = @"
                Structure Point
                    Private _X As Integer
                    Public Property X As Integer
                        Get
                            Return _X
                        End Get
                        Private Set(value As Integer)
                            _X = value
                        End Set
                    End Property

                    Private _Y As Integer
                    Public Property Y As Integer
                        Get
                            Return _Y
                        End Get
                        Private Set(value As Integer)
                            _Y = value
                        End Set
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

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("point.X");
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

            DiagnosticResult expected = VerifyVB.Diagnostic("FindReadOnlyByRef").WithLocation(0).WithArguments("point.X");
            await VerifyVB.VerifyAnalyzerAsync(test, expected);
        }
    }
}
