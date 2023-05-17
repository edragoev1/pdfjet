
package examples;

import java.io.*;
import com.pdfjet.*;

public class Example_99 {
    public Example_99() throws Exception {
		PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_99.pdf")));	  
		pdf.setCompliance(Compliance.PDF_UA);	

		Font f2 = new Font(pdf, CoreFont.HELVETICA);
		f2.setSize(14f);
	  
		Page page = new Page(pdf, A4.PORTRAIT);

		float PT2MM = 2.83465f;
		
		Font f1 = new Font(pdf, CoreFont.HELVETICA);		  
		f1.setSize(20f);
		
		TextBox textBox1 = new TextBox(f1);
		String strTest = "MurrayConsult One";
		textBox1.setLocation(50, 50); 
		textBox1.setWidth(100f * PT2MM);	
		textBox1.setHeight(15f * PT2MM);		
		textBox1.setMargin(0f);						
		textBox1.setBorders(true);
		textBox1.setSpacing(0f);
		textBox1.setTextAlignment(Align.LEFT);
		textBox1.setFgColor(Color.black);			  
		textBox1.setBgColor(Color.palegreen);			  
		textBox1.setVerticalAlignment(Align.BOTTOM);
		textBox1.setTextDirection(Direction.LEFT_TO_RIGHT);
		textBox1.setText(strTest);	
		float[] xy = textBox1.drawOn(page); 		  
 
		//	
		// 2. Vertical box
		//
		TextBox textBox2 = new TextBox(f2, "MurrayConsult");			
		textBox2.setLocation(50f, xy[1]);
		textBox2.setWidth(200f);			
		textBox2.setHeight(200f);		
		textBox2.setTextDirection(Direction.TOP_TO_BOTTOM);
		// textBox2.setMargin(0f);
		textBox2.setBorders(true);
		textBox2.setBgColor(Color.yellow);			  
		xy = textBox2.drawOn(page); 	
		
		Box box = new Box();
		box.setLocation(xy[0], xy[1]);
		box.setSize(20f, 20f);
		xy = box.drawOn(page);

		//
		// 3. Horizontal 2
		//
		Font f3 = new Font(pdf, CoreFont.HELVETICA);
		f3.setSize( 20f );		
		
		TextBox textBox3 = new TextBox(f3);	
		String strTest3 = "MurrayConsult Two";

		textBox3.setLocation(50, xy[1]); 
		textBox3.setWidth(100f * PT2MM);	
		textBox3.setHeight(15f * PT2MM);		

		textBox3.setMargin(0f);						
		textBox3.setBorders(true);
		textBox3.setSpacing(0f);
		textBox3.setTextAlignment(Align.LEFT);
		
		textBox3.setFgColor(Color.black);			  
		textBox3.setBgColor(Color.lightblue);			  
		 
		textBox3.setVerticalAlignment(Align.TOP);
		textBox3.setTextDirection(Direction.LEFT_TO_RIGHT);

		textBox3.setText(strTest3);	
		textBox3.drawOn(page); 		  
		  
		// Done  
      	pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        new Example_99();
    }
}
