using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using System.Data.Entity;

namespace EF6.Commands
{
    public class ContextTool
    {
        public static IEnumerable<Type> GetContextTypes(Assembly assembly) =>
            assembly.GetTypes().Where(
                t => !t.GetTypeInfo().IsAbstract
                     && !t.GetTypeInfo().IsGenericType
                     && typeof(DbContext).IsAssignableFrom(t));
        
        public static Type SelectType(IEnumerable<Type> types, String name)
        {
            Type[] candidates;

            if (String.IsNullOrEmpty(name))
            {
                candidates = types.Take(2).ToArray();
                if (candidates.Length == 0)
                {
                    throw new InvalidOperationException(Strings.NoContext);
                }
                if (candidates.Length == 1)
                {
                    return candidates[0];
                }

                throw new InvalidOperationException(Strings.MultipleContexts);
            }

            candidates = FilterTypes(types, name, ignoreCase: true).ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(Strings.NoContextWithName(name));
            }
            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            // Disambiguate using case
            candidates = FilterTypes(candidates, name).ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(Strings.MultipleContextsWithName(name));
            }
            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            // Allow selecting types in the default namespace
            candidates = candidates.Where(t => t.Namespace == null).ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(Strings.MultipleContextsWithQualifiedName(name));
            }

            Debug.Assert(candidates.Length == 1, "candidates.Length is not 1.");

            return candidates[0];
        }

        private static IEnumerable<Type> FilterTypes(IEnumerable<Type> types,
            String name,
            bool ignoreCase = false)
        {
            Debug.Assert(types != null, "types is null.");
            Debug.Assert(!String.IsNullOrEmpty(name), "name is null or empty.");

            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return types.Where(
                t => String.Equals(t.Name, name, comparisonType)
                     || String.Equals(t.FullName, name, comparisonType)
                     || String.Equals(t.AssemblyQualifiedName, name, comparisonType));
        }
    }
}