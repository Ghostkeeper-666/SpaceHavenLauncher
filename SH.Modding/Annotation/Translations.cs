using SH.Content.Enums;
using System.Collections.Generic;

namespace SH.Modding.Annotation;

public static class Translations
{
    public static IReadOnlyDictionary<ELanguage, string> LinkedBy { get; } = new Dictionary<ELanguage, string>()
    {
        [ELanguage.EN] = "Linked by",
        [ELanguage.ES] = "Vinculado por",
        [ELanguage.DE] = "Verknüpft von",
        [ELanguage.PL] = "Połączone przez",
        [ELanguage.KO] = "다음에 의해 연결됨",
        [ELanguage.IT] = "Collegato da",
        [ELanguage.CN] = "被以下对象链接",
        [ELanguage.FR] = "Lié par",
        [ELanguage.CS] = "Propojeno pomocí",
        [ELanguage.PTBR] = "Vinculado por",
        [ELanguage.RU] = "Связано через",
        [ELanguage.JA] = "リンク元",
        [ELanguage.TR] = "Bağlayan",
    };

    public static IReadOnlyDictionary<ELanguage, string> LinksTo { get; } = new Dictionary<ELanguage, string>()
    {
        [ELanguage.EN] = "Links to",
        [ELanguage.ES] = "Enlaza a",
        [ELanguage.DE] = "Verweist auf",
        [ELanguage.PL] = "Łączy z",
        [ELanguage.KO] = "다음으로 연결",
        [ELanguage.IT] = "Collega a",
        [ELanguage.CN] = "链接到：",
        [ELanguage.FR] = "Lien vers ",
        [ELanguage.CS] = "Odkazuje na",
        [ELanguage.PTBR] = "Aponta para",
        [ELanguage.RU] = "Ссылается на",
        [ELanguage.JA] = "リンク先",
        [ELanguage.TR] = "Bağlantı hedefi",
    };

    public static IReadOnlyDictionary<ELanguage, string> IndirectlyLinkedBy { get; } = new Dictionary<ELanguage, string>()
    {
        // Indirectly linked by
        [ELanguage.EN] = "Indirectly linked by",
        [ELanguage.ES] = "Vinculado indirectamente por",
        [ELanguage.DE] = "Indirekt verknüpft von",
        [ELanguage.PL] = "Pośrednio połączone przez",
        [ELanguage.KO] = "간접적으로 연결한 대상",
        [ELanguage.IT] = "Collegato indirettamente da",
        [ELanguage.CN] = "被以下对象间接链接：",
        [ELanguage.FR] = "Indirectement lié par ",
        [ELanguage.CS] = "Nepřímo propojeno pomocí",
        [ELanguage.PTBR] = "Vinculado indiretamente por",
        [ELanguage.RU] = "Косвенно связано через",
        [ELanguage.JA] = "間接リンク元",
        [ELanguage.TR] = "Dolaylı olarak bağlayan",
    };

    public static IReadOnlyDictionary<ELanguage, string> IndirectlyLinksTo { get; } = new Dictionary<ELanguage, string>()
    {
        // Indirectly linked by
        [ELanguage.EN] = "Indirectly links to",
        [ELanguage.ES] = "Enlaza indirectamente a",
        [ELanguage.DE] = "Verweist indirekt auf",
        [ELanguage.PL] = "Pośrednio łączy z",
        [ELanguage.KO] = "간접적으로 연결",
        [ELanguage.IT] = "Collega indirettamente a",
        [ELanguage.CN] = "间接链接到：",
        [ELanguage.FR] = "Lien indirect vers ",
        [ELanguage.CS] = "Nepřímo odkazuje na",
        [ELanguage.PTBR] = "Aponta indiretamente para",
        [ELanguage.RU] = "Косвенно ссылается на",
        [ELanguage.JA] = "間接リンク先",
        [ELanguage.TR] = "Dolaylı olarak bağlanır"
    };

    public static IReadOnlyDictionary<ELanguage, string> RootNode { get; } = new Dictionary<ELanguage, string>()
    {
        [ELanguage.EN] = "Root Node",
        [ELanguage.ES] = "Nodo raíz",
        [ELanguage.DE] = "Stammknoten",
        [ELanguage.PL] = "Węzły główne",
        [ELanguage.KO] = "루트 노드",
        [ELanguage.IT] = "Nodi radice",
        [ELanguage.CN] = "根节点",
        [ELanguage.FR] = "Nœuds racine",
        [ELanguage.CS] = "Kořenové uzly",
        [ELanguage.PTBR] = "Nós raiz",
        [ELanguage.RU] = "Корневые узлы",
        [ELanguage.JA] = "ルートノード",
        [ELanguage.TR] = "Kök Düğümler",
    };
}
