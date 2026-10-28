/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_13.swift
 */
public class Example_13 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_13.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Switzerland, in English and Chinese")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)

        // Chinese (Simplified) font
        let f2 = try Font(pdf, IBMPlexSansSC.Regular)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let column = TextColumn()
        column.setLocation(70.0, 70.0)
        column.setWidth(500.0)
        column.setLineSpacing(1.5)      // 1.5 x font height
        column.setParagraphSpacing(0.5) // 0.5 x line spacing

        let p1 = Paragraph()
        p1.add(TextLine(f1,
"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002."))
        column.addParagraph(p1)

        let chinese = "瑞士联邦于1291年作为三个州之间的防御联盟而建立。此后数年间，其他地区陆续加入了最初的三个州。1499年，瑞士联邦脱离神圣罗马帝国，获得独立。长期以来，欧洲各大国一直尊重瑞士的主权和中立地位，瑞士也没有卷入两次世界大战。过去半个世纪欧洲的政治和经济一体化，以及瑞士在许多联合国机构和国际组织中的作用，加强了瑞士与邻国的联系。然而，瑞士直到2002年才正式成为联合国会员国。"
        let xy = column.drawOn(page)

        // The same paragraph in Chinese, in a column of its own, set tighter: IBM Plex Sans SC has a
        // line gap of a whole em, which the lines of a paragraph are set with.
        let column2 = TextColumn()
        column2.setLocation(70.0, xy[1] + 12.0)
        column2.setWidth(500.0)
        column2.setLineSpacing(1.2)
        column2.addCJKParagraph(f2, chinese, "zh-Hans") // Read by a screen reader as Chinese
        column2.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_13.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_13()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_13 => \(String(format: "%4lld", time1 - time0)) ms")
