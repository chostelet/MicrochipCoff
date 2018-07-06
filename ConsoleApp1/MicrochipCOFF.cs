#region License
/* 
 * Copyright (C) 2018 Christian Hostelet.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static System.Console;

namespace MCoff
{

    public struct Constants
    {
        public const ushort MICROCHIPMAGIC = 0x1240;
        public const ushort OPTHEADERMAGIC = 0x5678;
        public const int SYMNMLEN = 8;
        public const long FILEHEADERSIZE = 20;
        public const long OPTHEADERSIZE = 18;
        public const long SECTIONENTRYSIZE = 40;
        public const long RELOCENTRYSIZE = 12;
        public const long LINENOENTRYSIZE = 16;
        public const long SYMBOLENTRYSIZE = 20;
        public const long AUXENTRYSIZE = 20;
    }

    [Flags]
    public enum RenderFlags : ulong
    {
        /// <summary> Physical end of function </summary>
        RDR_EFCN = 0x00000001,
        /// <summary> Null </summary>
        RDR_NULL = 0x00000002,
        /// <summary> Automatic variable </summary>
        RDR_AUTO = 0x00000004,
        /// <summary> External symbol </summary>
        RDR_EXT = 0x00000008,
        /// <summary> Static </summary>
        RDR_STAT = 0x00000010,
        /// <summary> Register variable </summary>
        RDR_REG = 0x00000020,
        /// <summary> External definition </summary>
        RDR_EXTDEF = 0x00000040,
        /// <summary> Label </summary>
        RDR_LABEL = 0x00000080,
        /// <summary> Undefined label </summary>
        RDR_ULABEL = 0x00000100,
        /// <summary> Member of structure </summary>
        RDR_MOS = 0x00000200,
        /// <summary> Function argument </summary>
        RDR_ARG = 0x00000400,
        /// <summary> Structure tag </summary>
        RDR_STRTAG = 0x00000800,
        /// <summary> Member of union </summary>
        RDR_MOU = 0x00001000,
        /// <summary> Union tag </summary>
        RDR_UNTAG = 0x00002000,
        /// <summary> Type definition </summary>
        RDR_TPDEF = 0x00004000,
        /// <summary> Undefined static </summary>
        RDR_USTATIC = 0x00008000,
        /// <summary> Enumeration tag </summary>
        RDR_ENTAG = 0x00010000,
        /// <summary> Member of enumeration </summary>
        RDR_MOE = 0x00020000,
        /// <summary> Register parameter </summary>
        RDR_REGPARM = 0x00040000,
        /// <summary> Bit field </summary>
        RDR_FIELD = 0x00080000,
        /// <summary> Automatic argument </summary>
        RDR_AUTOARG = 0x00100000,
        /// <summary> Dummy entry (end of block) </summary>
        RDR_LASTENT = 0x00200000,
        /// <summary> "bb" or "eb" </summary>
        RDR_BLOCK = 0x00400000,
        /// <summary> "bf" or "ef" </summary>
        RDR_FCN = 0x00800000,
        /// <summary> End of structure </summary>
        RDR_EOS = 0x01000000,
        /// <summary> File name </summary>
        RDR_FILE = 0x02000000,
        /// <summary> Line number reformatted as symbol table entry </summary>
        RDR_LINE = 0x04000000,
        /// <summary> Duplicate tag </summary>
        RDR_ALIAS = 0x08000000,
        /// <summary> External symbol in dmert public library </summary>
        RDR_HIDDEN = 0x10000000,
        /// <summary> End of file </summary>
        RDR_EOF = 0x20000000,
        /// <summary> Absolute listing on or off </summary>
        RDR_LIST = 0x40000000,
        /// <summary> Section </summary>
        RDR_SECTION = 0x80000000
    };

    public interface IRenderer
    {
        void Render();
    }

    public interface ISymbolEntry : IRenderer
    {
        ulong Index { get; }

        Storage_Class Storage { get; }

        string Name { get; }

        uint Value { get; }

        SymbolType SymbolType { get; }

        byte NumAuxEntries { get; }

    }

    public abstract class AuxEntryBase : IRenderer
    {
        public abstract void Render();
    }

    #region Flags

    /// <summary>
    /// File Header Flags.
    /// </summary>
    [Flags]
    public enum FileHdrFlags : ushort
    {

        /// <summary>Relocation information has been stripped from the COFF file.</summary>
        F_RELFLG = 0x0001,
        /// <summary>The file is executable and has no unresolved external symbols. </summary>
        F_EXEC = 0x0002,
        /// <summary>Line number information has been stripped from the COFF file.</summary>
        F_LNNO = 0x0004,
        /// <summary>The MPASM assembler object file is from absolute (as opposed to relocatable) assembly code.</summary>
        F_ABSOLUTE = 0x0010,
        /// <summary>Local symbols have been stripped from the COFF file. </summary>
        L_SYMS = 0x0080,
        /// <summary>The COFF file produced utilizing the Extended mode.</summary>
        F_EXTENDED18 = 0x4000,
        /// <summary>The COFF file is processor independent. </summary>
        F_GENERIC = 0x8000
    }

    /// <summary>
    /// Section type and content flags.
    /// The flags which define the section type and the section qualifiers are stored as bit fields
    /// in the Flags field.
    /// Masks are defined for the bit fields to ease access.
    /// </summary>
    [Flags]
    public enum SectionHdrFlags : uint
    {
        /// <summary> Section contains executable code. </summary>
        STYP_TEXT = 0x00020,
        /// <summary> Section contains initialized data. </summary>
        STYP_DATA = 0x00040,
        /// <summary> Section contains uninitialized data. </summary>
        STYP_BSS = 0x00080,
        /// <summary> Section contains initialized data for program memory. </summary>
        STYP_DATA_ROM = 0x00100,
        /// <summary>  Section is absolute. </summary>
        STYP_ABS = 0x01000,
        /// <summary> Section is shared across banks. </summary>
        STYP_SHARED = 0x02000,
        /// <summary> Section is overlaid with other sections of the same name from different object modules. </summary>
        STYP_OVERLAY = 0x04000,
        /// <summary> Section is available using access bit. </summary>
        STYP_ACCESS = 0x08000,
        /// <summary> Section contains the overlay activation record for a function. </summary>
        STYP_ACTREC = 0x10000
    }

    /// <summary>
    /// Relocation type, implementation defined values.
    /// </summary>
    public enum Reloc_Type : ushort
    {
        ///<summary> CALL instruction (first word only on PIC18) </summary>
        RELOCT_CALL = 1,
        ///<summary> GOTO instruction (first word only on PIC18) </summary>
        RELOCT_GOTO = 2,
        ///<summary> Second 8 bits of an address </summary>
        RELOCT_HIGH = 3,
        ///<summary> Low order 8 bits of an address </summary>
        RELOCT_LOW = 4,
        ///<summary> 5 bits of address for the P operand of a PIC17 MOVFP or MOVPF instruction </summary>
        RELOCT_P = 5,
        ///<summary> Generate the appropriate instruction to bank switch for a symbol </summary>
        RELOCT_BANKSEL = 6,
        ///<summary> Generate the appropriate instruction to page switch for a symbol </summary>
        RELOCT_PAGESEL = 7,
        ///<summary> 16 bits of an address </summary>
        RELOCT_ALL = 8,
        ///<summary> Generate indirect bank selecting instructions </summary>
        RELOCT_IBANKSEL = 9,
        ///<summary> 8 bits of address for the F operand of a PIC17 MOVFP or MOVPF instruction </summary>
        RELOCT_F = 10,
        ///<summary> File register address for TRIS instruction </summary>
        RELOCT_TRIS = 11,
        ///<summary> MOVLR bank PIC17 banking instruction </summary>
        RELOCT_MOVLR = 12,
        ///<summary> MOVLB PIC17 and PIC18 banking instruction </summary>
        RELOCT_MOVLB = 13,
        ///<summary> Second word of an PIC18 GOTO instruction </summary>
        RELOCT_GOTO2 = 14,
        ///<summary> Second word of an PIC18 CALL instruction </summary>
        RELOCT_CALL2 = 14,
        ///<summary> Source register of the PIC18 MOVFF instruction </summary>
        RELOCT_FF1 = 15,
        ///<summary> Destination register of the PIC18 MOVFF instruction </summary>
        RELOCT_FF2 = 16,
        ///<summary> Destination register of the PIC18 MOVSF instruction </summary>
        RELOCT_SF2 = 16,
        ///<summary> First word of the PIC18 LFSR instruction </summary>
        RELOCT_LFSR1 = 17,
        ///<summary> Second word of the PIC18 LFSR instruction </summary>
        RELOCT_LFSR2 = 18,
        ///<summary> PIC18 BRA instruction </summary>
        RELOCT_BRA = 19,
        ///<summary> PIC18 RCALL instruction </summary>
        RELOCT_RCALL = 19,
        ///<summary> PIC18 relative conditional branch instructions </summary>
        RELOCT_CONDBRA = 20,
        ///<summary> Highest order 8 bits of a 24-bit address </summary>
        RELOCT_UPPER = 21,
        ///<summary> PIC18 access bit </summary>
        RELOCT_ACCESS = 22,
        ///<summary> Selecting the correct page using WREG as scratch </summary>
        RELOCT_PAGESEL_WREG = 23,
        ///<summary> Selecting the correct page using bit set/clear instructions </summary>
        RELOCT_PAGESEL_BITS = 24,
        ///<summary> Size of a section </summary>
        RELOCT_SCNSZ_LOW = 25,
        ///<summary> Size of a section </summary>
        RELOCT_SCNSZ_HIGH = 26,
        ///<summary> Size of a section </summary>
        RELOCT_SCNSZ_UPPER = 27,
        ///<summary> Address of the end of a section </summary>
        RELOCT_SCNEND_LOW = 28,
        ///<summary> Address of the end of a section </summary>
        RELOCT_SCNEND_HIGH = 29,
        ///<summary> Address of the end of a section </summary>
        RELOCT_SCNEND_UPPER = 30,
        ///<summary> Address of the end of a section on LFSR </summary>
        RELOCT_SCNEND_LFSR1 = 31,
        ///<summary> Address of the end of a section on LFSR </summary>
        RELOCT_SCNEND_LFSR2 = 32,
        ///<summary> File register address for 4-bit TRIS instruction </summary>
        RELOCT_TRIS_4BIT = 33
    };

    /// <summary>
    /// Values that represent base symbol types.
    /// </summary>
    public enum Symbol_BaseType : byte
    {
        /// <summary> null </summary>
        T_NULL = 0,
        /// <summary> void </summary>
        T_VOID = 1,
        /// <summary> character </summary>
        T_CHAR = 2,
        /// <summary> short integer </summary>
        T_SHORT = 3,
        /// <summary> integer </summary>
        T_INT = 4,
        /// <summary> long integer </summary>
        T_LONG = 5,
        /// <summary> floating point </summary>
        T_FLOAT = 6,
        /// <summary> double length floating point </summary>
        T_DOUBLE = 7,
        /// <summary> structure </summary>
        T_STRUCT = 8,
        /// <summary> union </summary>
        T_UNION = 9,
        /// <summary> enumeration </summary>
        T_ENUM = 10,
        /// <summary> member of enumeration </summary>
        T_MOE = 11,
        /// <summary> unsigned character </summary>
        T_UCHAR = 12,
        /// <summary> unsigned short </summary>
        T_USHORT = 13,
        /// <summary> unsigned integer </summary>
        T_UINT = 14,
        /// <summary> unsigned long </summary>
        T_ULONG = 15,
        /// <summary> long double </summary>
        T_LNGDBL = 16,
        /// <summary> short long </summary>
        T_SLONG = 17,
        /// <summary> unsigned short long </summary>
        T_USLONG = 18,
    };

    public static class Symbol_BaseTypeEx
    {
        private static Dictionary<Symbol_BaseType, string> sbt2stg = new Dictionary<Symbol_BaseType, string>()
        {
            { Symbol_BaseType.T_NULL, "null" },
            { Symbol_BaseType.T_VOID, "void" },
            { Symbol_BaseType.T_CHAR, "char" },
            { Symbol_BaseType.T_SHORT, "short" },
            { Symbol_BaseType.T_INT, "int" },
            { Symbol_BaseType.T_LONG, "long" },
            { Symbol_BaseType.T_FLOAT, "float" },
            { Symbol_BaseType.T_DOUBLE, "double" },
            { Symbol_BaseType.T_STRUCT, "struct" },
            { Symbol_BaseType.T_UNION, "union" },
            { Symbol_BaseType.T_ENUM, "enum" },
            { Symbol_BaseType.T_MOE, "moe" },
            { Symbol_BaseType.T_UCHAR, "uchar" },
            { Symbol_BaseType.T_USHORT, "ushort" },
            { Symbol_BaseType.T_UINT, "uint" },
            { Symbol_BaseType.T_ULONG, "ulong" },
            { Symbol_BaseType.T_LNGDBL, "lngdbl" },
            { Symbol_BaseType.T_SLONG, "slong" },
            { Symbol_BaseType.T_USLONG, "uslong" }
        };

        public static string GetName(this Symbol_BaseType sbt, IList<AuxEntry> auxentries)
        {
            if (sbt2stg.TryGetValue(sbt, out var s))
            {
                switch (sbt)
                {
                    case Symbol_BaseType.T_STRUCT:
                    case Symbol_BaseType.T_UNION:
                    case Symbol_BaseType.T_ENUM:
                        if (auxentries != null && auxentries.Count > 0)
                        {
                            var auxe = auxentries[0];
                            var tagidx = BitConverter.ToUInt32(auxe.Content, 0);
                            if (tagidx != 0)
                            {
                                if (SymbolsTable.TryGetSymbol(tagidx, out var symb))
                                {
                                    s = s + $" {symb.Name}";
                                }
                                else
                                {
                                    s = s + $" [{tagidx}]";
                                }
                            }
                        }
                        break;
                }
                return s;
            }
            return $"Unknown symbol base type {sbt}";
        }
    }

    /// <summary>
    /// Values that represent derived symbol types. Pointers, arrays, and functions are handled via derived types
    /// </summary>
    public enum Symbol_DerivedType : byte
    {
        /// <summary> no derived type </summary>
        DT_NON = 0,
        /// <summary> pointer to data memory </summary>
        DT_RAMPTR = 1,
        /// <summary> function </summary>
        DT_FCN = 2,
        /// <summary> array </summary>
        DT_ARY = 3,
        /// <summary> pointer to program memory </summary>
        DT_ROMPTR = 4,
        /// <summary> far (24 bit) pointer to program memory </summary>
        DT_FARROMPTR = 5,
    };

    /// <summary>
    /// Values that represent symbol storage class.
    /// </summary>
    public enum Storage_Class : sbyte
    {
        /// <summary> Physical end of function </summary>
        C_EFCN = -1,
        /// <summary> Null </summary>
        C_NULL = 0,
        /// <summary> Automatic variable </summary>
        C_AUTO = 1,
        /// <summary> External symbol </summary>
        C_EXT = 2,
        /// <summary> Static </summary>
        C_STAT = 3,
        /// <summary> Register variable </summary>
        C_REG = 4,
        /// <summary> External definition </summary>
        C_EXTDEF = 5,
        /// <summary> Label </summary>
        C_LABEL = 6,
        /// <summary> Undefined label </summary>
        C_ULABEL = 7,
        /// <summary> Member of structure </summary>
        C_MOS = 8,
        /// <summary> Function argument </summary>
        C_ARG = 9,
        /// <summary> Structure tag </summary>
        C_STRTAG = 10,
        /// <summary> Member of union </summary>
        C_MOU = 11,
        /// <summary> Union tag </summary>
        C_UNTAG = 12,
        /// <summary> Type definition </summary>
        C_TPDEF = 13,
        /// <summary> Undefined static </summary>
        C_USTATIC = 14,
        /// <summary> Enumeration tag </summary>
        C_ENTAG = 15,
        /// <summary> Member of enumeration </summary>
        C_MOE = 16,
        /// <summary> Register parameter </summary>
        C_REGPARM = 17,
        /// <summary> Bit field </summary>
        C_FIELD = 18,
        /// <summary> Automatic argument </summary>
        C_AUTOARG = 19,
        /// <summary> Dummy entry (end of block) </summary>
        C_LASTENT = 20,
        /// <summary> "bb" or "eb" </summary>
        C_BLOCK = 100,
        /// <summary> "bf" or "ef" </summary>
        C_FCN = 101,
        /// <summary> End of structure </summary>
        C_EOS = 102,
        /// <summary> File name </summary>
        C_FILE = 103,
        /// <summary> Line number reformatted as symbol table entry </summary>
        C_LINE = 104,
        /// <summary> Duplicate tag </summary>
        C_ALIAS = 105,
        /// <summary> External symbol in dmert public library </summary>
        C_HIDDEN = 106,
        /// <summary> End of file </summary>
        C_EOF = 107,
        /// <summary> Absolute listing on or off </summary>
        C_LIST = 108,
        /// <summary> Section </summary>
        C_SECTION = 109
    };

    public static class Storage_ClassEx
    {
        private static Dictionary<Storage_Class, string> st2stg = new Dictionary<Storage_Class, string>()
        {
            { Storage_Class.C_EFCN, "physical end of function" },
            { Storage_Class.C_NULL, "null class" },
            { Storage_Class.C_AUTO, "automatic variable" },
            { Storage_Class.C_EXT, "external symbol" },
            { Storage_Class.C_STAT, "static" },
            { Storage_Class.C_REG, "register variable" },
            { Storage_Class.C_EXTDEF, "external definition" },
            { Storage_Class.C_LABEL, "label" },
            { Storage_Class.C_ULABEL, "undefined label" },
            { Storage_Class.C_MOS, "member of structure" },
            { Storage_Class.C_ARG, "function argument" },
            { Storage_Class.C_STRTAG, "structure tag" },
            { Storage_Class.C_MOU, "member of union" },
            { Storage_Class.C_UNTAG, "union tag" },
            { Storage_Class.C_TPDEF, "type definition" },
            { Storage_Class.C_USTATIC, "undefined static" },
            { Storage_Class.C_ENTAG, "enumeration tag" },
            { Storage_Class.C_MOE, "member of enumeration" },
            { Storage_Class.C_REGPARM, "register parameter" },
            { Storage_Class.C_FIELD, "bit field" },
            { Storage_Class.C_AUTOARG, "auto argument" },
            { Storage_Class.C_LASTENT, "dummy entry (end of block)" },
            { Storage_Class.C_BLOCK, ".bb or .eb" },
            { Storage_Class.C_FCN, ".bf or .ef" },
            { Storage_Class.C_EOS, "end of structure" },
            { Storage_Class.C_FILE, "file name" },
            { Storage_Class.C_LINE, "line number reformatted as symbol table entry" },
            { Storage_Class.C_ALIAS, "duplicate tag" },
            { Storage_Class.C_HIDDEN, "ext symbol" },
            { Storage_Class.C_EOF, "end of file" },
            { Storage_Class.C_LIST, "absolute listing toggle" },
            { Storage_Class.C_SECTION, "section" },
        };

        public static string GetName(this Storage_Class st)
        {
            if (st2stg.TryGetValue(st, out var s))
                return s;
            return $"Unknown storage class {st}";
        }
    }

    /// <summary>
    /// Values that represent Line Number Entry flags.
    /// </summary>
    public enum LineNoEntryFlags : ushort
    {
        LINENO_NOFCN = 0,
        /// <summary> Set if FuncnIndex is valid . </summary>
        LINENO_HASFCN = 1
    }

    /// <summary>
    /// Values that represent .file Entry flags.
    /// </summary>
    public enum AuxFileFlags : byte
    {
        X_FILE_NODEBUG = 0,
        /// <summary> This .file entry was included for debugging purposes only. </summary>
        X_FILE_DEBUG_ONLY = 1
    }

    #endregion

    #region COFF structures

    /// <summary>
    /// A packed string name.
    /// </summary>
    public struct RawPackedStringName
    {
        // public fixed byte s_name[Constants.SYMNMLEN];
        public readonly uint s_zeroes;
        public readonly uint s_offset;

        public RawPackedStringName(BinaryReader rd)
        {
            s_zeroes = rd.ReadUInt32();
            s_offset = rd.ReadUInt32();
        }

        public override string ToString()
        {
            if (s_zeroes == 0)
                return StringTable.GetString(s_offset);

            var s = string.Empty;
            byte[] b1 = BitConverter.GetBytes(s_zeroes);
            foreach (var b in b1)
            {
                if (b == 0)
                    return s;
                s += (char)b;
            }
            byte[] b2 = BitConverter.GetBytes(s_offset);
            foreach (var b in b2)
            {
                if (b == 0)
                    return s;
                s += (char)b;
            }
            return s;

        }
    }

    /// <summary>
    /// The Microchip COFF file header.
    /// </summary>
    public class FileHeader
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public FileHeader(BinaryReader rd)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            rd.BaseStream.Position = 0;
            Magic = rd.ReadUInt16();
            NumSections = rd.ReadUInt16();
            CreationTimeDate = epoch.AddSeconds(rd.ReadUInt32());
            SymbTablePtr = rd.ReadUInt32();
            NumSymbols = rd.ReadUInt32();
            OptHeaderSize = rd.ReadUInt16();
            Flags = (FileHdrFlags)rd.ReadUInt16();
        }

        public bool IsMicrochip => Magic == Constants.MICROCHIPMAGIC;

        /// <summary> The magic number of the COFF file. </summary>
        public readonly ushort Magic;

        /// <summary> The number of sections in the COFF file. </summary>
        public readonly ushort NumSections;

        /// <summary> The time and date stamp when the COFF file was created (this value is a count of the number of seconds since midnight January 1, 1970). </summary>
        public readonly DateTime CreationTimeDate;

        /// <summary> A pointer to the symbol table. </summary>
        public readonly uint SymbTablePtr;

        /// <summary> The number of entries in the symbol table. </summary>
        public readonly uint NumSymbols;

        /// <summary> The size of the optional header record.  </summary>
        public readonly ushort OptHeaderSize;

        /// <summary> Flags. Information on what is contained in the COFF file. </summary>
        public readonly FileHdrFlags Flags;

        public string RenderFlags
        {
            get
            {
                var flgs = new List<string>();
                if (Flags.HasFlag(FileHdrFlags.F_EXEC))
                    flgs.Add("executable");
                if (Flags.HasFlag(FileHdrFlags.F_ABSOLUTE))
                    flgs.Add("absolute");
                if (Flags.HasFlag(FileHdrFlags.F_RELFLG))
                    flgs.Add("no reloc");
                if (Flags.HasFlag(FileHdrFlags.L_SYMS))
                    flgs.Add("no symbol");
                if (Flags.HasFlag(FileHdrFlags.F_LNNO))
                    flgs.Add("no line number");
                if (Flags.HasFlag(FileHdrFlags.F_GENERIC))
                    flgs.Add("generic PIC");
                if (Flags.HasFlag(FileHdrFlags.F_EXTENDED18))
                    flgs.Add("extended PIC");
                return String.Join(", ", flgs);
            }
        }

    }

    /// <summary>
    /// Optional File Header.
    /// </summary>
    public class OptionalHeader
    {

        private static readonly Dictionary<uint, string> proctypemap =
            new Dictionary<uint, string>()
            {
                { 0x1230, "PIC18F1230" },
                { 0x1231, "PIC18F1231" },
                { 0x1330, "PIC18F1330" },
                { 0x1331, "PIC18F1331" },
                { 0x1824, "PIC16F1824" },
                { 0x1828, "PIC16F1828" },
                { 0x2221, "PIC18F2221" },
                { 0x2321, "PIC18F2321" },
                { 0x2331, "PIC18F2331" },
                { 0x2410, "PIC18F2410" },
                { 0x2420, "PIC18F2420" },
                { 0x2423, "PIC18F2423" },
                { 0x242F, "PIC18F242" },
                { 0x2431, "PIC18F2431" },
                { 0x2439, "PIC18F2439" },
                { 0x2450, "PIC18F2450" },
                { 0x2455, "PIC18F2455" },
                { 0x2458, "PIC18F2458" },
                { 0x2480, "PIC18F2480" },
                { 0x2510, "PIC18F2510" },
                { 0x2515, "PIC18F2515" },
                { 0x2520, "PIC18F2520" },
                { 0x2523, "PIC18F2523" },
                { 0x2525, "PIC18F2525" },
                { 0x252F, "PIC18F252" },
                { 0x2539, "PIC18F2539" },
                { 0x2550, "PIC18F2550" },
                { 0x2553, "PIC18F2553" },
                { 0x2580, "PIC18F2580" },
                { 0x2585, "PIC18F2585" },
                { 0x2610, "PIC18F2610" },
                { 0x2620, "PIC18F2620" },
                { 0x2680, "PIC18F2680" },
                { 0x2682, "PIC18F2682" },
                { 0x2685, "PIC18F2685" },
                { 0x4221, "PIC18F4221" },
                { 0x4321, "PIC18F4321" },
                { 0x4331, "PIC18F4331" },
                { 0x4410, "PIC18F4410" },
                { 0x4420, "PIC18F4420" },
                { 0x4423, "PIC18F4423" },
                { 0x442F, "PIC18F442" },
                { 0x4431, "PIC18F4431" },
                { 0x4439, "PIC18F4439" },
                { 0x4450, "PIC18F4450" },
                { 0x4455, "PIC18F4455" },
                { 0x4458, "PIC18F4458" },
                { 0x4480, "PIC18F4480" },
                { 0x4510, "PIC18F4510" },
                { 0x4515, "PIC18F4515" },
                { 0x4520, "PIC18F4520" },
                { 0x4523, "PIC18F4523" },
                { 0x4525, "PIC18F4525" },
                { 0x452F, "PIC18F452" },
                { 0x4539, "PIC18F4539" },
                { 0x4550, "PIC18F4550" },
                { 0x4553, "PIC18F4553" },
                { 0x4580, "PIC18F4580" },
                { 0x4585, "PIC18F4585" },
                { 0x4610, "PIC18F4610" },
                { 0x4620, "PIC18F4620" },
                { 0x4680, "PIC18F4680" },
                { 0x4682, "PIC18F4682" },
                { 0x4685, "PIC18F4685" },
                { 0x6310, "PIC18F6310" },
                { 0x6311, "PIC18F6311" },
                { 0x6390, "PIC18F6390" },
                { 0x6393, "PIC18F6393" },
                { 0x6410, "PIC18F6410" },
                { 0x6411, "PIC18F6411" },
                { 0x6490, "PIC18F6490" },
                { 0x6493, "PIC18F6493" },
                { 0x6511, "PIC18F6511" },
                { 0x6522, "PIC18F65K22" },
                { 0x6525, "PIC18F6525" },
                { 0x6527, "PIC18F6527" },
                { 0x6585, "PIC18F6585" },
                { 0x6627, "PIC18F6627" },
                { 0x6680, "PIC18F6680" },
                { 0x6693, "PIC18F66J93" },
                { 0x6721, "PIC18F6722" },
                { 0x6722, "PIC18F67K22" },
                { 0x6723, "PIC18F6723" },
                { 0x6790, "PIC18F67J90" },
                { 0x6793, "PIC18F6793" },
                { 0x8242, "PIC18C242" },
                { 0x8248, "PIC18F248" },
                { 0x8252, "PIC18C252" },
                { 0x8258, "PIC18F258" },
                { 0x8310, "PIC18F8310" },
                { 0x8311, "PIC18F8311" },
                { 0x8390, "PIC18F8390" },
                { 0x8393, "PIC18F8393" },
                { 0x8410, "PIC18F8410" },
                { 0x8411, "PIC18F8411" },
                { 0x8442, "PIC18C442" },
                { 0x8448, "PIC18F448" },
                { 0x8452, "PIC18C452" },
                { 0x8458, "PIC18F458" },
                { 0x8490, "PIC18F8490" },
                { 0x8493, "PIC18F8493" },
                { 0x8522, "PIC18F85K22" },
                { 0x8525, "PIC18F8525" },
                { 0x8527, "PIC18F8527" },
                { 0x8585, "PIC18F8585" },
                { 0x8601, "PIC18C601" },
                { 0x8621, "PIC18F8621" },
                { 0x8622, "PIC18F8622" },
                { 0x8625, "PIC18F8627" },
                { 0x8628, "PIC18F8628" },
                { 0x8658, "PIC18C658" },
                { 0x8672, "PIC18F86J72" },
                { 0x8680, "PIC18F8680" },
                { 0x8690, "PIC18F86J90" },
                { 0x8693, "PIC18F8693" },
                { 0x8721, "PIC18F8722" },
                { 0x8722, "PIC18F87K22" },
                { 0x8723, "PIC18F8723" },
                { 0x8772, "PIC18F87J72" },
                { 0x8790, "PIC18F87J90" },
                { 0x8793, "PIC18F87J93" },
                { 0x8801, "PIC18C801" },
                { 0x8858, "PIC18C858" },
                { 0xA122, "PIC18F1220" },
                { 0xA132, "PIC18F1320" },
                { 0xA133, "PIC18LF13K22" },
                { 0xA135, "PIC18F13K50" },
                { 0xA142, "PIC18LF14K22" },
                { 0xA145, "PIC18F14K50" },
                { 0xA222, "PIC18F2220" },
                { 0xA232, "PIC18F2320" },
                { 0xA422, "PIC18F4220" },
                { 0xA432, "PIC18F4320" },
                { 0xA580, "PIC18F25K80" },
                { 0xA621, "PIC18F6621" },
                { 0xA628, "PIC18F6628" },
                { 0xA652, "PIC18F6520" },
                { 0xA662, "PIC18F6620" },
                { 0xA672, "PIC18F6720" },
                { 0xA680, "PIC18F26K80" },
                { 0xA722, "PIC16F722A" },
                { 0xA723, "PIC16F723A" },
                { 0xA824, "PIC16LF1824" },
                { 0xA825, "PIC16LF1825" },
                { 0xA828, "PIC16LF1828" },
                { 0xA829, "PIC16LF1829" },
                { 0xA852, "PIC18F8520" },
                { 0xA862, "PIC18F8620" },
                { 0xA872, "PIC18F8720" },
                { 0xB132, "PIC18F13K22" },
                { 0xB142, "PIC18F14K22" },
                { 0xB322, "PIC18LF23K22" },
                { 0xB390, "PIC18F63J90" },
                { 0xB411, "PIC18LF24J11" },
                { 0xB422, "PIC18LF24K22" },
                { 0xB450, "PIC18LF24J50" },
                { 0xB490, "PIC18F64J90" },
                { 0xB510, "PIC18F65J10" },
                { 0xB511, "PIC18LF25J11" },
                { 0xB515, "PIC18F65J15" },
                { 0xB522, "PIC18LF25K22" },
                { 0xB550, "PIC18F65J50" },
                { 0xB551, "PIC18LF25J50" },
                { 0xB580, "PIC18F45K80" },
                { 0xB590, "PIC18F65J90" },
                { 0xB591, "PIC18F65K90" },
                { 0xB610, "PIC18F66J10" },
                { 0xB611, "PIC18F66J11" },
                { 0xB612, "PIC18LF26J11" },
                { 0xB615, "PIC18F66J15" },
                { 0xB616, "PIC18F66J16" },
                { 0xB617, "PIC18LF26J13" },
                { 0xB622, "PIC18F66K22" },
                { 0xB623, "PIC18LF26K22" },
                { 0xB650, "PIC18F66J50" },
                { 0xB651, "PIC18LF26J50" },
                { 0xB655, "PIC18F66J55" },
                { 0xB656, "PIC18LF26J53" },
                { 0xB660, "PIC18F66J60" },
                { 0xB665, "PIC18F66J65" },
                { 0xB680, "PIC18F46K80" },
                { 0xB690, "PIC18F66J90" },
                { 0xB691, "PIC18F66K90" },
                { 0xB710, "PIC18F67J10" },
                { 0xB711, "PIC18F67J11" },
                { 0xB712, "PIC18LF27J13" },
                { 0xB722, "PIC16LF722A" },
                { 0xB723, "PIC16LF723A" },
                { 0xB750, "PIC18F67J50" },
                { 0xB760, "PIC18F67J60" },
                { 0xB790, "PIC18F67K90" },
                { 0xC1825, "PIC16F1825" },
                { 0xC1829, "PIC16F1829" },
                { 0xC322, "PIC18LF43K22" },
                { 0xC390, "PIC18F83J90" },
                { 0xC411, "PIC18LF44J11" },
                { 0xC422, "PIC18LF44K22" },
                { 0xC450, "PIC18LF44J50" },
                { 0xC490, "PIC18F84J90" },
                { 0xC510, "PIC18F85J10" },
                { 0xC511, "PIC18F8511" },
                { 0xC515, "PIC18F85J15" },
                { 0xC522, "PIC18LF45K22" },
                { 0xC550, "PIC18F85J50" },
                { 0xC551, "PIC18LF45J50" },
                { 0xC580, "PIC18F65K80" },
                { 0xC590, "PIC18F85J90" },
                { 0xC591, "PIC18F85K90" },
                { 0xC610, "PIC18F86J10" },
                { 0xC611, "PIC18F86J11" },
                { 0xC612, "PIC18LF46J11" },
                { 0xC615, "PIC18F86J15" },
                { 0xC616, "PIC18F86J16" },
                { 0xC617, "PIC18LF46J13" },
                { 0xC622, "PIC18F86K22" },
                { 0xC623, "PIC18LF46K22" },
                { 0xC650, "PIC18F86J50" },
                { 0xC651, "PIC18LF46J50" },
                { 0xC655, "PIC18F86J55" },
                { 0xC656, "PIC18LF46J53" },
                { 0xC660, "PIC18F86J60" },
                { 0xC665, "PIC18F86J65" },
                { 0xC680, "PIC18F66K80" },
                { 0xC690, "PIC18F86K90" },
                { 0xC710, "PIC18F87J10" },
                { 0xC711, "PIC18F87J11" },
                { 0xC712, "PIC18LF47J13" },
                { 0xC750, "PIC18F87J50" },
                { 0xC751, "PIC18LF47J53" },
                { 0xC753, "PIC18LF27J53" },
                { 0xC760, "PIC18F87J60" },
                { 0xC790, "PIC18F87K90" },
                { 0xD135, "PIC18LF13K50" },
                { 0xD145, "PIC18LF14K50" },
                { 0xD320, "PIC18F23K20" },
                { 0xD322, "PIC18F23K22" },
                { 0xD410, "PIC18F24J10" },
                { 0xD411, "PIC18F24J11" },
                { 0xD420, "PIC18F24K20" },
                { 0xD422, "PIC18F24K22" },
                { 0xD450, "PIC18F24J50" },
                { 0xD510, "PIC18F25J10" },
                { 0xD511, "PIC18F25J11" },
                { 0xD520, "PIC18F25K20" },
                { 0xD522, "PIC18F25K22" },
                { 0xD550, "PIC18F25J50" },
                { 0xD580, "PIC18LF25K80" },
                { 0xD611, "PIC18F26J11" },
                { 0xD616, "PIC18F26J13" },
                { 0xD620, "PIC18F26K20" },
                { 0xD622, "PIC18F26K22" },
                { 0xD650, "PIC18F26J50" },
                { 0xD655, "PIC18F26J53" },
                { 0xD660, "PIC18F96J60" },
                { 0xD665, "PIC18F96J65" },
                { 0xD680, "PIC18LF26K80" },
                { 0xD711, "PIC18F27J13" },
                { 0xD720, "PIC16LF720" },
                { 0xD721, "PIC16LF721" },
                { 0xD750, "PIC18F27J53" },
                { 0xD760, "PIC18F97J60" },
                { 0xE320, "PIC18F43K20" },
                { 0xE322, "PIC18F43K22" },
                { 0xE410, "PIC18F44J10" },
                { 0xE411, "PIC18F44J11" },
                { 0xE420, "PIC18F44K20" },
                { 0xE422, "PIC18F44K22" },
                { 0xE450, "PIC18F44J50" },
                { 0xE510, "PIC18F45J10" },
                { 0xE511, "PIC18F45J11" },
                { 0xE520, "PIC18F45K20" },
                { 0xE522, "PIC18F45K22" },
                { 0xE550, "PIC18F45J50" },
                { 0xE580, "PIC18LF45K80" },
                { 0xE611, "PIC18F46J11" },
                { 0xE616, "PIC18F46J13" },
                { 0xE620, "PIC18F46K20" },
                { 0xE622, "PIC18F46K22" },
                { 0xE650, "PIC18F46J50" },
                { 0xE655, "PIC18F46J53" },
                { 0xE680, "PIC18LF46K80" },
                { 0xE711, "PIC18F47J13" },
                { 0xE750, "PIC18F47J53" },
                { 0xF580, "PIC18LF65K80" },
                { 0xF622, "PIC18F6622" },
                { 0xF680, "PIC18LF65K80" },
                { 0xF720, "PIC16F720" },
                { 0xF721, "PIC16F721" }
            };

        public OptionalHeader(BinaryReader rd, FileHeader fhdr)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            if (fhdr == null)
                throw new ArgumentNullException(nameof(fhdr));
            rd.BaseStream.Position = Constants.FILEHEADERSIZE;
            Magic = rd.ReadUInt16();
            var vstamp = rd.ReadUInt32();
            ProcessorType = rd.ReadUInt32();
            ROMWidthInBits = rd.ReadUInt32();
            RAMWidthInBits = rd.ReadUInt32();
            VersionStamp = $"{vstamp / 10000}.{(vstamp / 100) % 100}.{vstamp % 100}";
            ProcessorName = GetProcessorName(fhdr);
        }

        /// <summary> The magic number can be used to determine the appropriate layout. </summary>
        public readonly ushort Magic;

        /// <summary> Version stamp. </summary>
        public readonly string VersionStamp;

        /// <summary> Target processor type.. </summary>
        public readonly uint ProcessorType;

        /// <summary> Name of the processor. </summary>
        public readonly string ProcessorName;

        /// <summary> Width of program memory in bits. </summary>
        public readonly uint ROMWidthInBits;

        /// <summary> Width of data memory in bits. </summary>
        public readonly uint RAMWidthInBits;

        public bool IsValid => Magic == Constants.OPTHEADERMAGIC;

        private string GetProcessorName(FileHeader fhdr)
        {
            if (fhdr.Flags.HasFlag(FileHdrFlags.F_GENERIC))
            {
                if (fhdr.Flags.HasFlag(FileHdrFlags.F_EXTENDED18))
                    return "the generic Extended PIC18 processor (PIC18F4620)";
                return "the generic Legacy PIC18 processor (PIC18C452)";
            }

            if (proctypemap.TryGetValue(ProcessorType, out var procname))
                return $"the '{procname}'";
            return "an unknown processor";
        }

    }

    /// <summary>
    /// Section Header.
    /// </summary>
    public class SectionHeader : IRenderer
    {

        public const short N_FILE = -3;
        public const short N_DEBUG = -2;
        public const short N_ABS = -1;
        public const short N_UNDEF = 0;

        public readonly long SectPos;

        /// <summary> Physical address of the section. </summary>
        public readonly uint PhysAddr;

        /// <summary> Virtual address of the section. Always contains the same value as <see cref="PhysAddr"/>. </summary>
        public readonly uint VirtualAddr;

        /// <summary> Size of this section. </summary>
        public readonly uint SectionSize;

        /// <summary> Pointer to the raw data in the COFF file for this section. </summary>
        public readonly uint SectionPtr;

        /// <summary> Pointer to the relocation information in the COFF file for this section. </summary>
        public readonly uint RelocPtr;

        /// <summary> Pointer to the line number information in the COFF file for this section. </summary>
        public readonly uint LineNoPtr;

        /// <summary> The number of relocation entries for this section. </summary>
        public readonly ushort NumReloc;

        /// <summary> The number of line number entries for this section. </summary>
        public readonly ushort NumLineNo;

        /// <summary> Section type and content flags. </summary>
        public readonly SectionHdrFlags Flags;

        /// <summary> Section name. </summary>
        public readonly string SectionName;

        public SectionHeader(BinaryReader rd, long pos)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            SectPos = pos;
            SectionName = new RawPackedStringName(rd).ToString();
            PhysAddr = rd.ReadUInt32();
            VirtualAddr = rd.ReadUInt32();
            SectionSize = rd.ReadUInt32();
            SectionPtr = rd.ReadUInt32();
            RelocPtr = rd.ReadUInt32();
            LineNoPtr = rd.ReadUInt32();
            NumReloc = rd.ReadUInt16();
            NumLineNo = rd.ReadUInt16();
            Flags = (SectionHdrFlags)rd.ReadUInt32();
        }

        public void Render()
        {
            WriteLine($"{SectionName,-25} @0x{PhysAddr:X8} [size={SectionSize,-10} relocs={NumReloc,-5} lines={NumLineNo,-5}] ({RenderFlags})");
        }

        public string RenderFlags
        {
            get
            {
                var flgs = new List<string>();
                if (Flags.HasFlag(SectionHdrFlags.STYP_ABS))
                    flgs.Add("absolute");
                if (Flags.HasFlag(SectionHdrFlags.STYP_ACCESS))
                    flgs.Add("access RAM");
                if (Flags.HasFlag(SectionHdrFlags.STYP_ACTREC))
                    flgs.Add("activation record");
                if (Flags.HasFlag(SectionHdrFlags.STYP_BSS))
                    flgs.Add("bss");
                if (Flags.HasFlag(SectionHdrFlags.STYP_DATA))
                    flgs.Add("idata");
                if (Flags.HasFlag(SectionHdrFlags.STYP_DATA_ROM))
                    flgs.Add("ROM data");
                if (Flags.HasFlag(SectionHdrFlags.STYP_OVERLAY))
                    flgs.Add("overlay");
                if (Flags.HasFlag(SectionHdrFlags.STYP_SHARED))
                    flgs.Add("shared RAM");
                if (Flags.HasFlag(SectionHdrFlags.STYP_TEXT))
                    flgs.Add("text");
                return string.Join(", ", flgs);
            }
        }

    }

    /// <summary>
    /// Relocation Entry.
    /// </summary>
    public class Reloc_Entry : IRenderer
    {
        /// <summary> Address of reference (byte offset relative to start of raw data). </summary>
        public readonly uint VirtualAddr;

        /// <summary> Index into symbol table. </summary>
        public readonly uint SymbolIndex;

        /// <summary> Signed offset to be added to the address of symbol <see cref="SymbolIndex"/>. </summary>
        public readonly short Offset;

        /// <summary> Relocation type, implementation defined values. </summary>
        public readonly Reloc_Type RelocType;

        public Reloc_Entry(BinaryReader rd)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            VirtualAddr = rd.ReadUInt32();
            SymbolIndex = rd.ReadUInt32();
            Offset = rd.ReadInt16();
            RelocType = (Reloc_Type)rd.ReadUInt16();
        }

        public void Render()
        {
            WriteLine($"Reloc {RelocType}");
        }
    }

    public class SymbolType
    {
        private const int X_DIMNUM = 4;

        public Symbol_BaseType BaseType { get; }

        public Symbol_DerivedType[] DerivedTypes { get; }

        private readonly IList<AuxEntry> auxEntries;

        public SymbolType()
        {
            BaseType = Symbol_BaseType.T_NULL;
            DerivedTypes = null;
        }

        public SymbolType(uint type, IList<AuxEntry> auxentries)
        {
            BaseType = GetBaseType(type);
            DerivedTypes = GetDerivedTypes(type);
            auxEntries = auxentries;
        }

        public string Type2String(string SymbolName)
        {
            if (BaseType == Symbol_BaseType.T_NULL)
                return SymbolName;
            var sb = new StringBuilder();
            sb.Append($"{BaseType.GetName(auxEntries)} ");
            bool nameused = false;
            if (DerivedTypes != null)
            {
                int xdimnum = 0;
                for (int i = DerivedTypes.Count() - 1; i >= 0; i--)
                {
                    switch (DerivedTypes[i])
                    {
                        case Symbol_DerivedType.DT_ARY:
                            sb.Append(nameused ? "" : SymbolName);
                            nameused = true;
                            sb.Append("[");
                            if (xdimnum < X_DIMNUM)
                            {
                                ushort dim = BitConverter.ToUInt16(auxEntries[0].Content, 8 + 2 * xdimnum++);
                                sb.Append($"{dim}");
                            }
                            sb.Append("]");
                            break;

                        case Symbol_DerivedType.DT_FCN:
                            sb.Append(nameused ? "" : SymbolName);
                            nameused = true;
                            sb.Append("()");
                            break;

                        case Symbol_DerivedType.DT_RAMPTR:
                            sb.Append("ram *");
                            break;

                        case Symbol_DerivedType.DT_ROMPTR:
                            sb.Append("rom *");
                            break;

                        case Symbol_DerivedType.DT_FARROMPTR:
                            sb.Append("far rom *");
                            break;
                    }
                }

            }
            sb.Append(nameused ? "" : SymbolName);
            return sb.ToString();
        }

        private Symbol_BaseType GetBaseType(uint n_type)
            => (Symbol_BaseType)((byte)(n_type & 0x1f));

        private Symbol_DerivedType[] GetDerivedTypes(uint n_type)
        {
            var lderiv = new List<Symbol_DerivedType>();
            uint derivs = (n_type >> 5);
            while (derivs != 0)
            {
                lderiv.Add((Symbol_DerivedType)(derivs & 7));
                derivs >>= 3;
            }
            if (lderiv.Count > 0)
                return lderiv.ToArray();
            return null;
        }

    }

    /// <summary>
    /// Symbol Table Entry.
    /// </summary>
    public abstract class SymbolEntryBase : ISymbolEntry
    {
        protected readonly SectionInfo SectInfo;

        private static RenderFlags NoRenders =
        //            RenderFlags.RDR_ALIAS |
        //            RenderFlags.RDR_ARG |
        //            RenderFlags.RDR_AUTO |
        //            RenderFlags.RDR_AUTOARG |
                RenderFlags.RDR_BLOCK |
        //            RenderFlags.RDR_EFCN |
        //            RenderFlags.RDR_ENTAG |
                RenderFlags.RDR_EOF |
        //            RenderFlags.RDR_EOS |
        //            RenderFlags.RDR_EXT |
        //            RenderFlags.RDR_EXTDEF |
                RenderFlags.RDR_FCN |
        //            RenderFlags.RDR_FIELD |
                RenderFlags.RDR_FILE |
                RenderFlags.RDR_HIDDEN |
        //            RenderFlags.RDR_LABEL |
        //            RenderFlags.RDR_LASTENT |
                RenderFlags.RDR_LINE |
                RenderFlags.RDR_LIST |
        //            RenderFlags.RDR_MOE |
        //            RenderFlags.RDR_MOS |
        //            RenderFlags.RDR_MOU |
                RenderFlags.RDR_NULL |
        //            RenderFlags.RDR_REG |
        //            RenderFlags.RDR_REGPARM |
                RenderFlags.RDR_SECTION |
        //            RenderFlags.RDR_STAT |
        //            RenderFlags.RDR_STRTAG |
        //            RenderFlags.RDR_TPDEF |
        //            RenderFlags.RDR_ULABEL |
        //            RenderFlags.RDR_UNTAG |
        //            RenderFlags.RDR_USTATIC |
            0
            ;

        public ulong Index { get; }

        public abstract Storage_Class Storage { get; }

        public abstract RenderFlags RenderMask { get; }

        public virtual string Name => String.Empty;

        public virtual uint Value => 0;

        public abstract byte NumAuxEntries { get; }

        public SymbolType SymbolType { get; protected set; } = new SymbolType();

        protected SymbolEntryBase(ulong idx)
        {
            Index = idx;
            SectInfo = new SectionInfo(n_scnum);
        }

        public void Render()
        {
            if (!NoRenders.HasFlag(RenderMask))
            {
                Write($"{Index,5}: {Storage.GetName(),25} {NumAuxEntries} ");
                RenderDetails();
            }
        }

        public abstract void RenderDetails();

        static protected string n_name;
        static protected uint n_value;
        static protected short n_scnum;
        static protected uint n_type;
        static protected Storage_Class n_sclass;
        static protected List<AuxEntry> auxentries;

        public static ISymbolEntry GetSymbolEntry(BinaryReader rd, ulong index, out byte nentries)
        {
            n_name = new RawPackedStringName(rd ?? throw new ArgumentNullException(nameof(rd))).ToString();
            n_value = rd.ReadUInt32();
            n_scnum = rd.ReadInt16();
            n_type = rd.ReadUInt32();
            n_sclass = (Storage_Class)rd.ReadSByte();
            nentries = rd.ReadByte();
            auxentries = new List<AuxEntry>(nentries);
            for (int j = 0; j < nentries; j++)
            {
                auxentries.Add(new AuxEntry(rd));
            }

            switch (n_sclass)
            {
                case Storage_Class.C_FILE:
                    return new FILE_Symbol(index);

                case Storage_Class.C_ALIAS:
                    return new ALIAS_Symbol(index);

                case Storage_Class.C_ARG:
                    return new ARG_Symbol(index);

                case Storage_Class.C_AUTO:
                    return new AUTO_Symbol(index);

                case Storage_Class.C_AUTOARG:
                    return new AUTOARG_Symbol(index);

                case Storage_Class.C_BLOCK:
                    return new BLOCK_Symbol(index);

                case Storage_Class.C_EFCN:
                    return new EFCN_Symbol(index);

                case Storage_Class.C_ENTAG:
                    return new ENTAG_Symbol(index);

                case Storage_Class.C_EOF:
                    return new EOF_Symbol(index);

                case Storage_Class.C_EOS:
                    return new EOS_Symbol(index);

                case Storage_Class.C_EXT:
                    return new EXT_Symbol(index);

                case Storage_Class.C_EXTDEF:
                    return new EXTDEF_Symbol(index);

                case Storage_Class.C_FCN:
                    return new FCN_Symbol(index);

                case Storage_Class.C_FIELD:
                    return new FIELD_Symbol(index);

                case Storage_Class.C_HIDDEN:
                    return new HIDDEN_Symbol(index);

                case Storage_Class.C_LABEL:
                    return new LABEL_Symbol(index);

                case Storage_Class.C_LASTENT:
                    return new LASTENT_Symbol(index);

                case Storage_Class.C_LINE:
                    return new LINE_Symbol(index);

                case Storage_Class.C_LIST:
                    return new LIST_Symbol(index);

                case Storage_Class.C_MOE:
                    return new MOE_Symbol(index);

                case Storage_Class.C_MOS:
                    return new MOS_Symbol(index);

                case Storage_Class.C_MOU:
                    return new MOU_Symbol(index);

                case Storage_Class.C_NULL:
                    return new NULL_Symbol(index);

                case Storage_Class.C_REG:
                    return new REG_Symbol(index);

                case Storage_Class.C_REGPARM:
                    return new REGPARM_Symbol(index);

                case Storage_Class.C_SECTION:
                    return new SECTION_Symbol(index);

                case Storage_Class.C_STAT:
                    return new STAT_Symbol(index);

                case Storage_Class.C_STRTAG:
                    return new STRTAG_Symbol(index);

                case Storage_Class.C_TPDEF:
                    return new TPDEF_Symbol(index);

                case Storage_Class.C_ULABEL:
                    return new ULABEL_Symbol(index);

                case Storage_Class.C_UNTAG:
                    return new UNTAG_Symbol(index);

                case Storage_Class.C_USTATIC:
                    return new USTATIC_Symbol(index);

            }

            return null;
        }

        protected Symbol_BaseType GetBaseType => (Symbol_BaseType)((byte)(n_type & 0x1f));

        protected Symbol_DerivedType[] GetDerivedTypes
        {
            get
            {
                var lderiv = new List<Symbol_DerivedType>();
                uint derivs = (n_type >> 5);
                while (derivs != 0)
                {
                    lderiv.Add((Symbol_DerivedType)(derivs & 7));
                    derivs >>= 3;
                }
                if (lderiv.Count > 0)
                    return lderiv.ToArray();
                return null;
            }
        }

    }

    public class ALIAS_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_ALIAS;

        public override RenderFlags RenderMask => RenderFlags.RDR_ALIAS;

        public override byte NumAuxEntries => 0;

        public ALIAS_Symbol(ulong idx) :base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class ARG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_ARG;

        public override RenderFlags RenderMask => RenderFlags.RDR_ARG;

        public override byte NumAuxEntries => 0;

        public ARG_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class AUTO_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_AUTO;

        public override RenderFlags RenderMask => RenderFlags.RDR_AUTO;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public AUTO_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            Name = n_name;
            Value = n_value;
            if (!SectInfo.IsAbsolute)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}' at offset 0x{Value:X} (stack-relative)");
        }

    }

    public class AUTOARG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_AUTOARG;

        public override RenderFlags RenderMask => RenderFlags.RDR_AUTOARG;

        public override byte NumAuxEntries => 0;

        public AUTOARG_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class BLOCK_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_BLOCK;

        public override RenderFlags RenderMask => RenderFlags.RDR_BLOCK;

        public override byte NumAuxEntries => 1;

        public ushort LineNo { get; }

        public uint NextSymbIndex { get; }

        public bool IsEnd { get; }

        public BLOCK_Symbol(ulong idx) : base(idx)
        {
            IsEnd = (n_name == ".eb" ?
                        true :
                        (n_name == ".bb" ?
                            false :
                            throw new InvalidDataException($"{Storage} : wrong name for Block symbol: {n_name}.")));
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            var aux = auxentries[0];
            LineNo = BitConverter.ToUInt16(aux.Content, 4);
            NextSymbIndex = IsEnd ? 0 : BitConverter.ToUInt32(aux.Content, 12);
        }

        public override void RenderDetails()
        {
            if (IsEnd)
            {
                WriteLine($"End-block at line#{LineNo}");
            }
            else
            {
                WriteLine($"Begin-block at line#{LineNo} in section '{SectInfo.Name}', next symbol at 0x{NextSymbIndex:X}");
            }
        }

    }

    public class EFCN_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_EFCN;

        public override RenderFlags RenderMask => RenderFlags.RDR_EFCN;

        public override byte NumAuxEntries => 0;

        public EFCN_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class ENTAG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_ENTAG;

        public override RenderFlags RenderMask => RenderFlags.RDR_ENTAG;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public override uint Value { get; }

        public ushort Size { get; }

        public uint EndNextIndex { get; }

        public ENTAG_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : invalid section number '{SectInfo.SectNum}'.");
            Name = n_name;
            Value = n_value;
            var cont = auxentries[0].Content;
            Size = BitConverter.ToUInt16(cont, 6);
            EndNextIndex = BitConverter.ToUInt32(cont, 12);
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}', {Size} byte(s)");
        }

    }

    public class EOF_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_EOF;

        public override RenderFlags RenderMask => RenderFlags.RDR_EOF;

        public override byte NumAuxEntries => 0;

        public EOF_Symbol(ulong idx) : base(idx)
        {
            if (n_name != ".eof")
                throw new InvalidDataException($"{Storage} : invalid name '{n_name}'.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : wrong section number {SectInfo.SectNum}.");
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
        }

        public override void RenderDetails()
        {
            WriteLine("End-of-file");
        }

    }

    public class EOS_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_EOS;

        public override RenderFlags RenderMask => RenderFlags.RDR_EOS;

        public override byte NumAuxEntries => 1;

        public uint StructIndex { get; }

        public ushort Size { get; }

        public EOS_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            var aux = auxentries[0];
            StructIndex = BitConverter.ToUInt32(aux.Content, 0);
            Size = BitConverter.ToUInt16(aux.Content, 6);
        }

        public override void RenderDetails()
        {
            if (SymbolsTable.TryGetSymbol(StructIndex, out var symb))
            {
                WriteLine($"Total size of '{symb.Name}' is {Size} byte(s)");
                return;
            }
            WriteLine($"Total size = {Size} byte(s)");
        }

    }

    public class EXT_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_EXT;

        public override RenderFlags RenderMask => RenderFlags.RDR_EXT;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public EXT_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            Name = n_name;
            Value = n_value;
            if (SectInfo.IsDebug || SectInfo.IsFile)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            if (SectInfo.IsUndefined)
            {
                WriteLine($"extern '{SymbolType.Type2String(Name)}' = 0x{Value:X}");
                return;
            }
            if (SectInfo.IsAbsolute)
            {
                WriteLine($"'{SymbolType.Type2String(Name)}' = 0x{Value:X} (Abs)");
                return;
            }
            WriteLine($"global '{SymbolType.Type2String(Name)}' at 0x{Value:X} in section '{SectInfo.Name}'");
        }

    }

    public class EXTDEF_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_EXTDEF;

        public override RenderFlags RenderMask => RenderFlags.RDR_EXTDEF;

        public override byte NumAuxEntries => 0;

        public EXTDEF_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class FCN_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_FCN;

        public override RenderFlags RenderMask => RenderFlags.RDR_FCN;

        public override byte NumAuxEntries => 1;

        public bool IsEnd { get; }

        public override uint Value { get; }

        public uint SymbIndex { get; }

        public uint StructSize { get; }

        public uint LineNo { get; }

        public uint NextSymbIndex { get; }

        public short ActiveSectionNum { get; }

        public FCN_Symbol(ulong idx) : base(idx)
        {
            IsEnd = (n_name == ".ef" ?
                        true :
                        (n_name == ".bf" ?
                            false :
                            throw new InvalidDataException($"{Storage} : wrong name for Block symbol: {n_name}.")));

            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            Value = n_value;
            var cont = auxentries[0].Content;
            SymbIndex = BitConverter.ToUInt32(cont, 0);
            StructSize = BitConverter.ToUInt32(cont, 4);
            LineNo = BitConverter.ToUInt32(cont, 8);
            NextSymbIndex = IsEnd ? 0 : BitConverter.ToUInt32(cont, 12);
            ActiveSectionNum = BitConverter.ToInt16(cont, 16);
        }

        public override void RenderDetails()
        {
            if (IsEnd)
            {
                WriteLine($"End-function at 0x{Value:X}, line#{LineNo}");
            }
            else
            {
                WriteLine($"Begin-function at 0x{Value:X} in section '{SectInfo.Name}', line#{LineNo}, next symbol at 0x{NextSymbIndex:X}");
            }
        }

    }

    public class FIELD_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_FIELD;

        public override RenderFlags RenderMask => RenderFlags.RDR_FIELD;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public override uint Value { get; }

        public ushort Size { get; }

        public FIELD_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            Name = n_name;
            Value = n_value;
            Size = BitConverter.ToUInt16(auxentries[0].Content, 6);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}', {Size} bit(s) wide at offset {Value}");
        }

    }

    public class FILE_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_FILE;

        public override RenderFlags RenderMask => RenderFlags.RDR_FILE;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public override uint Value { get; }

        public FILE_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : wrong section number {SectInfo.SectNum}.");
            var btype = (byte)(n_type & 0x3F);
            if (btype != 0)
                throw new InvalidDataException($"{Storage} : invalid base type for this symbol: {btype}.");
            var cont = auxentries[0].Content;
            var off = BitConverter.ToUInt32(cont, 0);
            Value = BitConverter.ToUInt32(cont, 4);
            Name = StringTable.GetString(off);
        }

        public override void RenderDetails()
        {
            if (Value > 0)
            {
                WriteLine($"'{Name}' included at line #{Value}");
            }
            else
            {
                WriteLine($"'{Name}'");
            }
        }

    }

    public class HIDDEN_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_HIDDEN;

        public override RenderFlags RenderMask => RenderFlags.RDR_HIDDEN;

        public override byte NumAuxEntries => 0;

        public HIDDEN_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class LABEL_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_LABEL;

        public override RenderFlags RenderMask => RenderFlags.RDR_LABEL;

        public override byte NumAuxEntries => 0;

        public override string Name { get; }

        public override uint Value { get; }

        public LABEL_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (SectInfo.IsDebug || SectInfo.IsFile || SectInfo.IsUndefined)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
        }

        public override void RenderDetails()
        {
            if (SectInfo.IsAbsolute)
            {
                WriteLine($"label '{Name}' = 0x{Value:X} (Abs)");
                return;
            }
            WriteLine($"label '{Name}' at 0x{Value:X} in section '{SectInfo.Name}'");
        }

    }

    public class LASTENT_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_LASTENT;

        public override RenderFlags RenderMask => RenderFlags.RDR_LASTENT;

        public override byte NumAuxEntries => 0;

        public LASTENT_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine("End-of-block");
        }

    }

    public class LINE_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_LINE;

        public override RenderFlags RenderMask => RenderFlags.RDR_LINE;

        public override byte NumAuxEntries => 0;

        public LINE_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class LIST_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_LIST;

        public override RenderFlags RenderMask => RenderFlags.RDR_LIST;

        public override byte NumAuxEntries => 0;

        public override string Name { get; }

        public override uint Value { get; }

        public LIST_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
        }

        public override void RenderDetails()
        {
            WriteLine($"{Name} at line #{Value}");
        }

    }

    public class MOE_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_MOE;

        public override RenderFlags RenderMask => RenderFlags.RDR_MOE;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public MOE_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            if (!SectInfo.IsAbsolute)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}' at offset 0x{Value:X}");
        }

    }

    public class MOS_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_MOS;

        public override RenderFlags RenderMask => RenderFlags.RDR_MOS;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public MOS_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            if (!SectInfo.IsAbsolute)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}' at offset 0x{Value:X}");
        }

    }

    public class MOU_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_MOU;

        public override RenderFlags RenderMask => RenderFlags.RDR_MOU;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public MOU_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            if (!SectInfo.IsAbsolute)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}' at offset 0x{Value:X}");
        }

    }

    public class NULL_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_NULL;

        public override RenderFlags RenderMask => RenderFlags.RDR_NULL;

        public override byte NumAuxEntries => 0;

        public NULL_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class REG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_REG;

        public override RenderFlags RenderMask => RenderFlags.RDR_REG;

        public override byte NumAuxEntries => 0;

        public REG_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class REGPARM_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_REGPARM;

        public override RenderFlags RenderMask => RenderFlags.RDR_REGPARM;

        public override byte NumAuxEntries => 0;

        public REGPARM_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class SECTION_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_SECTION;

        public override RenderFlags RenderMask => RenderFlags.RDR_SECTION;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public uint SectionLen { get; }
        public ushort NumReloc { get; }
        public ushort NumLineNo { get; }

        public SECTION_Symbol(ulong idx) : base(idx)
        {
            Name = n_name;
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            var cont = auxentries[0].Content;
            SectionLen = BitConverter.ToUInt32(cont, 0);
            NumReloc = BitConverter.ToUInt16(cont, 4);
            NumLineNo = BitConverter.ToUInt16(cont, 6);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{Name}' of {SectionLen} byte(s) [#reloc={NumReloc}, #lineno={NumLineNo}]");
        }

    }

    public class STAT_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_STAT;

        public override RenderFlags RenderMask => RenderFlags.RDR_STAT;

        public override byte NumAuxEntries { get; }

        public override string Name { get; }

        public override uint Value { get; }

        public STAT_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count > 1)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            NumAuxEntries = (byte)auxentries.Count;
            if (SectInfo.IsDebug || SectInfo.IsFile)
                throw new InvalidDataException($"{Storage} : invalid section number {SectInfo.SectNum}.");
            Name = n_name;
            Value = n_value;
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            if (SectInfo.IsUndefined)
            {
                WriteLine($"extern static '{SymbolType.Type2String(Name)}' = 0x{Value:X}");
                return;
            }
            if (SectInfo.IsAbsolute)
            {
                WriteLine($"'{SymbolType.Type2String(Name)}' = 0x{Value:X} (Abs)");
                return;
            }
            WriteLine($"static '{SymbolType.Type2String(Name)}' at 0x{Value:X} in section '{SectInfo.Section.SectionName}'");
        }

    }

    public class STRTAG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_STRTAG;

        public override RenderFlags RenderMask => RenderFlags.RDR_STRTAG;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public override uint Value { get; }

        public ushort Size { get; }

        public uint EndNextIndex { get; }

        public STRTAG_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : invalid section number '{SectInfo.SectNum}'.");
            Name = n_name;
            Value = n_value;
            var cont = auxentries[0].Content;
            Size = BitConverter.ToUInt16(cont, 6);
            EndNextIndex = BitConverter.ToUInt32(cont, 12);
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}', {Size} byte(s)");
        }

    }

    public class TPDEF_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_TPDEF;

        public override RenderFlags RenderMask => RenderFlags.RDR_TPDEF;

        public override byte NumAuxEntries => 0;

        public TPDEF_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class ULABEL_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_ULABEL;

        public override RenderFlags RenderMask => RenderFlags.RDR_ULABEL;

        public override byte NumAuxEntries => 0;

        public ULABEL_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }

    public class UNTAG_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_UNTAG;

        public override RenderFlags RenderMask => RenderFlags.RDR_UNTAG;

        public override byte NumAuxEntries => 1;

        public override string Name { get; }

        public override uint Value { get; }

        public ushort Size { get; }

        public uint EndNextIndex { get; }

        public UNTAG_Symbol(ulong idx) : base(idx)
        {
            if (auxentries.Count != NumAuxEntries)
                throw new InvalidDataException($"{Storage} : wrong number of auxiliary symbol entries.");
            if (!SectInfo.IsDebug)
                throw new InvalidDataException($"{Storage} : invalid section number '{SectInfo.SectNum}'.");
            Name = n_name;
            Value = n_value;
            var cont = auxentries[0].Content;
            Size = BitConverter.ToUInt16(cont, 6);
            EndNextIndex = BitConverter.ToUInt32(cont, 12);
            SymbolType = new SymbolType(n_type, auxentries);
        }

        public override void RenderDetails()
        {
            WriteLine($"'{SymbolType.Type2String(Name)}', {Size} byte(s)");
        }

    }

    public class USTATIC_Symbol : SymbolEntryBase
    {
        public override Storage_Class Storage => Storage_Class.C_USTATIC;

        public override RenderFlags RenderMask => RenderFlags.RDR_USTATIC;

        public override byte NumAuxEntries => 0;

        public USTATIC_Symbol(ulong idx) : base(idx)
        {
        }

        public override void RenderDetails()
        {
            WriteLine();
        }

    }


    /// <summary>
    /// Line Number Entry
    /// </summary>
    public class COFF_LineNumber : IRenderer
    {
        /// <summary> Symbol table index of associated source file. </summary>
        public readonly uint SymTableIndex;

        /// <summary> Line number. </summary>
        public readonly ushort LineNo;

        /// <summary> Address of code for this line number entry. </summary>
        public readonly uint PhysAddr;

        /// <summary> Bit flags for the line number entry. </summary>
        public readonly LineNoEntryFlags Flags;

        /// <summary> Symbol table index of associated function (if there is one). </summary>
        public readonly uint FuncnIndex;

        public COFF_LineNumber(BinaryReader rd)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            SymTableIndex = rd.ReadUInt32();
            LineNo = rd.ReadUInt16();
            PhysAddr = rd.ReadUInt32();
            Flags = (LineNoEntryFlags)rd.ReadUInt16();
            FuncnIndex = rd.ReadUInt32();
        }

        public void Render()
        {
            WriteLine();
            var fn = Flags == LineNoEntryFlags.LINENO_HASFCN ? $" in function index {FuncnIndex}" : "";
            WriteLine($"Line #{LineNo} at 0x{PhysAddr:X}{fn}");
        }

    }

    public class AuxEntry : AuxEntryBase
    {
        public readonly byte[] Content;

        public AuxEntry(AuxEntry e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            Content = new byte[Constants.AUXENTRYSIZE];
            Array.Copy(e.Content, Content, Constants.AUXENTRYSIZE);
        }

        public AuxEntry(BinaryReader rd)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            Content = rd.ReadBytes((int)Constants.AUXENTRYSIZE);
        }

        public override void Render()
        {
            WriteLine("   Raw Auxiliary Entry:");
            var s1 = String.Join(" ", Content.Take((int)Constants.AUXENTRYSIZE / 2).Select((b) => $"{b:X2}"));
            WriteLine($"      {s1}");
            ;
            var s2 = String.Join(" ", Content.Skip((int)Constants.AUXENTRYSIZE / 2).Select((b) => $"{b:X2}"));
            WriteLine($"      {s2}");
            ;
        }

        protected virtual void Xlat(AuxEntry e)
        {
        }

    }

    /// <summary>
    /// Auxiliary Symbol Table Entry for Function Call References.
    /// </summary>
    public sealed class AuxFcnCalls : AuxEntry
    {
        public const uint AUX_FCN_CALLS_HIGHERORDER = (uint.MaxValue);
        public const uint Not_An_Interrupt = 0;
        public const uint Low_Priority = 1;
        public const uint High_Priority = 2;

        /// <summary>
        /// Symbol index of the called function. If call of a higher order function, set to AUX_FCN_CALLS_HIGHERORDER.
        /// </summary>
        public uint CalleeIndex { get; private set; }

        /// <summary> Specifies whether the function is an interrupt, and if so, the priority of the interrupt. </summary>
        public uint IsInterrupt { get; private set; }

        public AuxFcnCalls(BinaryReader rd) : base(rd) => Xlat(this);
        public AuxFcnCalls(AuxEntry e) : base(e) => Xlat(e);

        public override void Render()
        {
            WriteLine($"      Callee symbol index={CalleeIndex}");
            if (IsInterrupt > 0)
            {
                WriteLine($"      {(IsInterrupt > 1 ? "high" : "low")} priority interrupt");
            }
            base.Render();
        }

        protected override void Xlat(AuxEntry e)
        {
            CalleeIndex = BitConverter.ToUInt32(e.Content, 0);
            IsInterrupt = BitConverter.ToUInt32(e.Content, 4);
        }

    }

    /// <summary>
    /// Auxiliary Symbol Table Entry for a Variable of Type struct/union/enum.
    /// </summary>
    public sealed class AuxVar : AuxEntry
    {
        /// <summary>
        /// Symbol index of a structure, union or enumerated tag..
        /// </summary>
        public uint SymbIndex { get; private set; }

        /// <summary>
        /// Size of the structure, union or enumeration.
        /// </summary>
        public ushort Size { get; private set; }

        public AuxVar(BinaryReader rd) : base(rd) => Xlat(this);
        public AuxVar(AuxEntry e) : base(e) => Xlat(e);

        public override void Render()
        {
            WriteLine($"       Symbol index={SymbIndex}");
            WriteLine($"       Symbol size={Size}.");
            base.Render();
        }

        protected override void Xlat(AuxEntry e)
        {
            SymbIndex = BitConverter.ToUInt32(e.Content, 0);
            Size = BitConverter.ToUInt16(e.Content, 6);
        }

    }

    public sealed class SectionInfo
    {
        public readonly short SectNum;
        public readonly string Name;
        public readonly SectionHeader Section;
        public readonly bool IsAbsolute;
        public readonly bool IsDebug;
        public readonly bool IsFile;
        public readonly bool IsUndefined;

        public SectionInfo(short scnum)
        {
            SectNum = scnum;
            Section = null;
            IsAbsolute = false;
            IsDebug = false;
            IsFile = false;
            IsUndefined = false;
            Name = SectionsTable.GetSectionName(scnum);

            switch (scnum)
            {
                case SectionHeader.N_ABS:
                    IsAbsolute = true;
                    break;

                case SectionHeader.N_DEBUG:
                    IsDebug = true;
                    break;

                case SectionHeader.N_FILE:
                    IsFile = true;
                    break;

                case SectionHeader.N_UNDEF:
                    IsUndefined = true;
                    break;

                default:
                    Section = SectionsTable.GetSection(scnum) ?? throw new IndexOutOfRangeException($"Unknown section number {scnum}.");
                    break;
            }

        }
    }

    #endregion

    #region Factories

    /// <summary>
    /// The string table.
    /// </summary>
    public static class StringTable
    {
        private static Dictionary<ulong, string> table;

        public static void LoadStringTable(BinaryReader rd, FileHeader fh)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            if (fh == null)
                throw new ArgumentNullException(nameof(fh));
            table = new Dictionary<ulong, string>();
            rd.BaseStream.Position = fh.SymbTablePtr + fh.NumSymbols * Constants.SYMBOLENTRYSIZE;
            uint nstrings = rd.ReadUInt32();
            if (nstrings > 0)
            {
                var buffer = rd.ReadBytes((int)nstrings);
                uint offset = 0;
                while (offset < buffer.Length)
                {
                    var s = String.Empty;
                    var idx = offset;
                    while (idx <= buffer.Length)
                    {
                        byte b = buffer[idx++];
                        if (b == 0)
                            break;
                        s += (char)b;
                    }
                    table.Add(offset + sizeof(uint), s);
                    offset = idx;
                }
            }
        }

        public static string GetString(uint offset)
        {
            if (table.TryGetValue(offset, out var s))
                return s;
            return String.Empty;
        }

        public static string GetString(this RawPackedStringName ps)
        {
            if (ps.s_zeroes == 0)
                return GetString(ps.s_offset);
            return ps.ToString();
        }

    }

    /// <summary>
    /// The sections table.
    /// </summary>
    public static class SectionsTable
    {
        public static readonly SortedList<ushort, SectionHeader> table = new SortedList<ushort, SectionHeader>();

        public static void LoadSectionsTable(BinaryReader rd, FileHeader fh)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            if (fh == null)
                throw new ArgumentNullException(nameof(fh));
            table.Clear();
            long pos = Constants.FILEHEADERSIZE + fh.OptHeaderSize;
            rd.BaseStream.Position = pos;

            for (ushort i = 0; i < fh.NumSections; i++)
            {
                var shdr = new SectionHeader(rd, pos);
                table.Add(i, shdr);
                pos += Constants.SECTIONENTRYSIZE;
            }
        }

        public static SectionHeader GetSection(short num)
        {
            if ((num > 0) && table.TryGetValue((ushort)(num - 1), out var scn))
                return scn;
            return null;
        }

        public static string GetSectionName(short num)
        {
            switch (num)
            {
                case SectionHeader.N_FILE:
                    return ".file";
                case SectionHeader.N_DEBUG:
                    return "<debug>";
                case SectionHeader.N_ABS:
                    return "<abs>";
                case SectionHeader.N_UNDEF:
                    return "<undef>";
            }
            var scn = GetSection(num);
            if (scn != null)
                return scn.SectionName;
            throw new InvalidOperationException($"Invalid section number: {num}");
        }

        public static string GetSectionType(short num)
        {
            switch (num)
            {
                case SectionHeader.N_FILE:
                case SectionHeader.N_DEBUG:
                case SectionHeader.N_ABS:
                case SectionHeader.N_UNDEF:
                    return num.ToString();
            }
            var scn = GetSection(num);
            if (scn != null)
            {
                return scn.RenderFlags;
            }
            throw new InvalidOperationException($"Invalid section number: {num}");
        }

        public static IEnumerable<SectionHeader> Sections()
        {
            foreach (var s in table)
                yield return s.Value;
        }

    }

    /// <summary>
    /// The symbols table.
    /// </summary>
    public static class SymbolsTable
    {

        public static readonly Dictionary<ulong, ISymbolEntry> table = new Dictionary<ulong, ISymbolEntry>();

        public static void LoadSymbolsTable(BinaryReader rd, FileHeader fh)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            if (fh == null)
                throw new ArgumentNullException(nameof(fh));
            table.Clear();

            rd.BaseStream.Position = fh.SymbTablePtr;

            ISymbolEntry symb;

            for (ulong idx = 0; idx < fh.NumSymbols; idx++)
            {
                symb = SymbolEntryBase.GetSymbolEntry(rd, idx, out byte nentries);
                table.Add(symb.Index, symb);
                idx += nentries;
            }
        }

        public static IEnumerable<ISymbolEntry> Symbols()
        {
            foreach (var s in table)
                yield return s.Value;
        }

        public static bool TryGetSymbol(ulong index, out ISymbolEntry symb)
            => table.TryGetValue(index, out symb);

    }

    #endregion

    public class MicrochipCOFFFile : IRenderer
    {

        private static MicrochipCOFFFile mcf;

        private MicrochipCOFFFile(BinaryReader rd)
        {
            FileHeader = new FileHeader(rd);
            if (!FileHeader.IsMicrochip)
                throw new NotSupportedException("This COFF file format is not supported.");
            if (FileHeader.OptHeaderSize != Constants.OPTHEADERSIZE)
                throw new NotSupportedException("This Microchip COFF file format is not supported.");
            rd.BaseStream.Seek(Constants.FILEHEADERSIZE, SeekOrigin.Begin);
            OptHeader = new OptionalHeader(rd, FileHeader);
        }

        public FileHeader FileHeader { get; }

        public OptionalHeader OptHeader { get; }

        public uint Num_Sections => FileHeader.NumSections;

        public uint Num_Symbols => FileHeader.NumSymbols;

        public static MicrochipCOFFFile Create(BinaryReader rd)
        {
            if (rd == null)
                throw new ArgumentNullException(nameof(rd));
            if (!rd.BaseStream.CanRead)
                throw new InvalidOperationException("Unreadable binary file.");
            if (!rd.BaseStream.CanSeek)
                throw new InvalidOperationException("Unseekable binary file.");
            mcf = new MicrochipCOFFFile(rd);
            if (mcf != null)
            {
                StringTable.LoadStringTable(rd, mcf.FileHeader);
                SectionsTable.LoadSectionsTable(rd, mcf.FileHeader);
                SymbolsTable.LoadSymbolsTable(rd, mcf.FileHeader);
            }
            return mcf;
        }

        public IEnumerable<SectionHeader> Sections => SectionsTable.Sections();

        public IEnumerable<ISymbolEntry> Symbols => SymbolsTable.Symbols();

        public void Render()
        {
            WriteLine($"Microchip COFF binary file (magic/opt = 0x{FileHeader.Magic:X}/0x{OptHeader.Magic:X}).");
            WriteLine($"MPLINK Linker {OptHeader.VersionStamp}, Linker.");
            WriteLine($"COFF file created {FileHeader.CreationTimeDate}");
            WriteLine($"File is {FileHeader.RenderFlags}.");
            WriteLine($"Number of sections: {FileHeader.NumSections}");
            WriteLine($"Number of symbols: {FileHeader.NumSymbols}");
            WriteLine($"Target processor is {OptHeader.ProcessorName} (code 0x{OptHeader.ProcessorType:X})).");
            WriteLine($"ROM Memory bits width: {OptHeader.ROMWidthInBits}");
            WriteLine($"RAM Memory bits width: {OptHeader.RAMWidthInBits}");

            WriteLine();
            WriteLine("SECTIONS:");
            WriteLine();
            foreach (IRenderer scn in Sections)
            {
                scn.Render();
            }

            WriteLine();
            WriteLine("SYMBOLS:");
            WriteLine();
            foreach (ISymbolEntry sym in Symbols)
            {
                sym.Render();
            }
        }

    }

}
