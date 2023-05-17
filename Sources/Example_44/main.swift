import Foundation
import PDFjet

/**
 *  Example_44.swift
 */
public class Example_44 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_44.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA)          // Latin font
        let f2 = Font(pdf, CJKFont.ST_HEITI_SC_LIGHT)   // Chinese (Simplified) font

        f1.setSize(12.0)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let rotate = 0 // degrees
        let column = TextColumn(rotate)
        column.setLocation(70.0, 70.0)
        column.setWidth(500.0)
        column.setSpaceBetweenLines(5.0)
        column.setSpaceBetweenParagraphs(10.0)

        let p1 = Paragraph()
        p1.add(TextLine(f1,
"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002."))
        column.addParagraph(p1)

        let chinese = "用于附属装置选择器应用程序的分析方法和计算基于与物料、附属装置和机器有关的不同参数和变量。 此类参数和变量的准确性会影响附属装置选择 应用程序结果的准确性。 虽然沃尔沃建筑设备公司、其子公司和附属公司（统称“沃尔沃建筑设备”）已尽力确保附属装置选择应用程序的准确性，但沃尔沃建筑设备无法保证结果准确无误。 因此，沃尔沃建筑设备并未对附属装置选择应用程序结果的准确性作出保证或声明（无论明示或暗示）。 沃尔沃建筑设备特此声明不对有关附属装置选择应用程序报告，此处获得的结果，或用于附属装置选择应用程序的设备的适宜性的所有明示或暗示保证，包括但不限于用于特定目的的适销性或合适性的任何保证承担任何责任。 在任何情况下，沃尔沃建筑设备均不对为实体或由实体创建的结果（或任何其它实体）以及由此造成的任何间接、意外、必然或特殊损害，包括但不限于损失收益或利润承担任何责任。"
        column.addChineseParagraph(f2, chinese)
        column.drawOn(page)

        pdf.complete()
    }
}   // End of Example_44.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_44()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_44", time0, time1)
