using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyInspector.Helpers
{
    internal static class AssemblyHelpers
    {
        public static bool IsAttributeOfType(MetadataReader reader, CustomAttribute customAttribute, string namespaceName, string typeName)
        {
            switch (customAttribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                        TypeDefinitionHandle parent = method.GetDeclaringType();
                        TypeDefinition typeDef = reader.GetTypeDefinition(parent);
                        return reader.StringComparer.Equals(typeDef.Namespace, namespaceName) && reader.StringComparer.Equals(typeDef.Name, typeName);
                    }

                case HandleKind.MemberReference:
                    {
                        MemberReference memberReference = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                        switch (memberReference.Parent.Kind)
                        {
                            case HandleKind.TypeReference:
                                TypeReference typeRef = reader.GetTypeReference((TypeReferenceHandle)memberReference.Parent);

                                string ns = reader.GetString(typeRef.Namespace);
                                string tp = reader.GetString(typeRef.Name);
                                return StringComparer.OrdinalIgnoreCase.Equals(ns, namespaceName) &&
                                    StringComparer.OrdinalIgnoreCase.Equals(tp, typeName);/* reader.StringComparer.Equals(typeRef.Namespace, namespaceName) && reader.StringComparer.Equals(typeRef.Name, typeName);
*/
                            case HandleKind.TypeDefinition:
                                TypeDefinition typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)memberReference.Parent);
                                return reader.StringComparer.Equals(typeDef.Namespace, namespaceName) && reader.StringComparer.Equals(typeDef.Name, typeName);

                            default:
                                return false; // constructor is global method -- no parent type to match.
                        }
                    }

                default:
                    throw new BadImageFormatException("Invalid custom attribute.");
            }
        }

        public static bool IsVisible(MetadataReader metaReader, TypeDefinition currentType)
        {
            TypeDefinitionHandle typeDefHandle = currentType.GetDeclaringType();

            // the visibility is not really a mask until you mask it with the VisibilityMask.
            TypeAttributes visibility = currentType.Attributes & TypeAttributes.VisibilityMask;

            if (typeDefHandle.IsNil)
            {
                return visibility.HasFlag(TypeAttributes.Public);
            }

            return visibility.HasFlag(TypeAttributes.Public) &
                IsVisible(metaReader, metaReader.GetTypeDefinition(typeDefHandle));
        }

        //TODO: should replace the AssembyName.GetAssemblyVersion with something based on SMR.
        public static AssemblyName GetAssemblyName(MetadataReader metaReader, AssemblyDefinition assemblyDef)
        {
            AssemblyName result = new AssemblyName();
            result.CultureInfo = string.IsNullOrEmpty(metaReader.GetString(assemblyDef.Culture)) ?
                null :
                new System.Globalization.CultureInfo(metaReader.GetString(assemblyDef.Culture));

            result.Name = metaReader.GetString(assemblyDef.Name);
            result.Version = assemblyDef.Version;
            result.SetPublicKey(metaReader.GetBlobBytes(assemblyDef.PublicKey));

            return result;
        }

        public static string GetTypeName(MetadataReader metaReader, ExportedType expTp)
        {
            string name = metaReader.GetString(expTp.Name);
            string ns = metaReader.GetString(expTp.Namespace);
            string id = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

            if (expTp.Implementation.Kind == HandleKind.ExportedType)
            {
                // we need to get the type of the parent as well.
                return GetTypeName(metaReader, metaReader.GetExportedType(((ExportedTypeHandle)expTp.Implementation))) + "." + id;
            }

            return id;
        }

        public static AssemblyReference GetAssemblyReferenceForExportedType(MetadataReader metaReader, ExportedType exportedType)
        {
            if (exportedType.Implementation.Kind == HandleKind.AssemblyReference)
            {
                return metaReader.GetAssemblyReference((AssemblyReferenceHandle)exportedType.Implementation);
            }
            else if (exportedType.Implementation.Kind == HandleKind.ExportedType)
            {
                // this means we have an exported nested type.
                /*
                .class extern forwarder System.TimeZoneInfo
                {
                    .assembly extern mscorlib
                }
                .class extern AdjustmentRule
                {
                    .class extern System.TimeZoneInfo
                }
               */
                return GetAssemblyReferenceForExportedType(metaReader, metaReader.GetExportedType(((ExportedTypeHandle)exportedType.Implementation)));
            }

            return default(AssemblyReference);
        }

        public static AssemblyReference GetAssemblyReferenceForReferencedType(MetadataReader metaReader, TypeReference typeRef)
        {
            if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                return metaReader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
            }
            else if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return GetAssemblyReferenceForReferencedType(metaReader, metaReader.GetTypeReference(((TypeReferenceHandle)typeRef.ResolutionScope)));
            }
            else if (typeRef.ResolutionScope.Kind == HandleKind.ModuleReference)
            {
                // hmmm.... IJW?
            }

            return default(AssemblyReference);
        }

        public static string GetTypeName(MetadataReader metaReader, TypeDefinition currentType)
        {
            string name = metaReader.GetString(currentType.Name);
            string ns = metaReader.GetString(currentType.Namespace);
            string Id = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

            TypeDefinitionHandle typeDefHandle = currentType.GetDeclaringType();

            if (typeDefHandle.IsNil)
            {
                return Id;
            }
            return $"{GetTypeName(metaReader, metaReader.GetTypeDefinition(typeDefHandle))}.{Id}";
        }

        public static string GetTypeName(MetadataReader metaReader, TypeReference typeRef)
        {
            string name = metaReader.GetString(typeRef.Name);
            string ns = metaReader.GetString(typeRef.Namespace);
            string id = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                // we need to get the type of the parent as well.
                return GetTypeName(metaReader, metaReader.GetTypeReference(((TypeReferenceHandle)typeRef.ResolutionScope))) + "." + id;
            }

            return id;

        }

        public static string FormatPublicKeyToken(MetadataReader metadataReader, BlobHandle handle)
        {
            byte[] bytes = metadataReader.GetBlobBytes(handle);

            if (bytes == null || bytes.Length <= 0)
            {
                return "null";
            }

            if (bytes.Length > 8)  // Strong named assembly
            {
                // Get the public key token, which is the last 8 bytes of the SHA-1 hash of the public key 
                using (var sha1 = SHA1.Create())
                {
                    var token = sha1.ComputeHash(bytes);

                    bytes = new byte[8];
                    int count = 0;
                    for (int i = token.Length - 1; i >= token.Length - 8; i--)
                    {
                        bytes[count] = token[i];
                        count++;
                    }
                }
            }

            // Convert bytes to string, but we don't want the '-' characters and need it to be lower case
            return BitConverter.ToString(bytes)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}
