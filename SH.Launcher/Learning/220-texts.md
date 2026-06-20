# Space Haven Basics: Text

Texts in Space Haven are basically identified by text **id**, and are read during gameplay from the **texts** XML file

The texts XML file:
- Supports specific pre-defined languages
- Has entries for any text which should be displayed in a different language

All text MUST obey [XML special character rule](https://www.ibm.com/docs/en/tsafm/4.1.1?topic=reference-xml-special-characters):
- Replace all **ampersand** characters '&' by `&amp;`
- Replace all **less than** characters '<' by `&lt;`
- Replace all **greather than** characters '>' by `&gt;`
- Replace all **double quotes** characters '"' by `&quot;`

# The "texts" XML file

It is as simple as it could possibly be:

```
<t>
...
  <t id="1717" pid="1710">
    <EN>Free Time &amp; Eating Preference</EN>
    <ES>Preferencias para el tiempo libre y las comidas</ES>
    <DE>Freizeit- und Essortvorliebe</DE>
    <PL>Preferencje: czas wolny i jedzenie</PL>
    <KO>자유 시간 및 식사 기본 설정</KO>
    <IT>Preferenza pasti e tempo libero</IT>
    <CN>休闲与就餐偏好</CN>
    <FR>Lieu de temps libre et de repas préféré</FR>
    <CS>Preference volného času a jídla</CS>
    <PTBR>Tempo livre e preferências alimentares</PTBR>
    <RU>Прием пищи и свободное время</RU>
    <JA>自由時間＆食事優先度</JA>
    <TR>Serbest Zaman ve Yemek Yeme Tercihi</TR>
  </t>
...
</t>
```

# The &lt;t&gt; node

- Attribute **id** is mandatory and uniquely identifies the text - it is used by the **haven** file
- Attribute **pid** is mandatory - (*NOT DOCUMENTED*) - modders usually copy its value from another similar text)

# Language nodes

- **EN** for English
- **ES** for Spanish
- **DE** for German
- **PL** for Polish
- **KO** for Korean
- **IT** for Italian
- **CN** for Simplified Chinese
- **FR** for French
- **CS** for Czech
- **PTBR** for Brazilian Portuguese
- **RU** for Russian
- **JA** for Japanese
- **TR** for Turkish
