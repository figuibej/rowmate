using System;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace RowMate.Tests
{
    // Categoría de test con guiones (AC-BASE-10, QA-V-05, LT-03…) que también corre dentro de Unity (AC-BASE-08).
    // Hallazgo del primer open real (It. 2, 00-primer-open.md §4): el NUnit 3.5 de Unity (com.unity.ext.nunit 2.0.5) invalida
    // en CategoryAttribute.ApplyToTest todo test cuya categoría tenga ',', '!', '+' o '-' ("Category name must not contain
    // ',', '!', '+' or '-'": 464 tests NotRunnable en la primera corrida). El NUnit 3.14 de dotnet ya no lo hace, y
    // RowMate.Tests.UnityApiCheck solo compila contra 3.5, no corre. Todos los IDs de AC, QA y LT llevan guiones.
    // Esta clase re-implementa IApplyToTest sin esa validación y agrega la misma propiedad "Category": los filtros
    // (-testCategory de Unity, --filter TestCategory=… de dotnet) y la matriz AC → test (run-tests.ps1 -Matrix) no cambian.
    // Está en el namespace RowMate.Tests A PROPÓSITO: dentro de los archivos de test (namespace RowMate.Tests.*),
    // [Category("…")] resuelve a esta clase antes que a NUnit.Framework.CategoryAttribute, que entra por el using del
    // archivo (C# busca primero en los namespaces que contienen al código y recién después en los using del archivo).
    // Así las fuentes de test no cambian. Lo controlan TestCategoryTests (Tests/EditMode/Project).
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public sealed class CategoryAttribute : NUnit.Framework.CategoryAttribute, IApplyToTest
    {
        public CategoryAttribute(string name)
            : base(name)
        {
        }

        void IApplyToTest.ApplyToTest(Test test)
        {
            test.Properties.Add(PropertyNames.Category, Name);
        }
    }
}
