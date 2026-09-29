// Xuất hóa đơn PDF cho kế toán: Accounts -> Tạo Bill -> [Export PDF] -> Invoice.pdf.
// jsPDF mặc định (Helvetica/WinAnsi) không hiển thị được dấu tiếng Việt
// ("Nguyễn Văn A" sẽ thành ký tự rác) nên hóa đơn dùng font Roboto (Apache 2.0) nhúng vào file.
// Nhãn trên hóa đơn song ngữ Việt (Anh) + dòng mô tả "hóa đơn của cái gì" để
// khách hàng lẫn nhân viên đều hiểu hóa đơn này cho khoản gì, kỳ nào.
import { jsPDF } from 'jspdf';
import type { Bill } from '../types/nexus';
import fontRegularUrl from './fonts/Roboto-Regular.ttf';
import fontBoldUrl from './fonts/Roboto-Bold.ttf';

export interface InvoicePdfData {
  invoiceNumber: string;
  /** Mô tả hóa đơn (tiếng Việt): gói cước nào, kỳ nào, gồm những khoản gì. */
  description: string;
  /** Kỳ tính cước dạng gốc ("September 2026") — dùng cho dòng phụ đề tiếng Anh. */
  billingPeriod: string;
  customerName: string;
  accountId: string;
  planName: string;
  equipmentName: string;
  planCharge: number;
  equipmentCharge: number;
  discountPercent: number;
  discountAmount: number;
  securityDeposit: number;
  serviceTaxRate: number;
  serviceTaxAmount: number;
  totalAmount: number;
  amountPaid: number;
  dueAmount: number;
  generatedBy: string;
  date: string;
}

const FONT_FAMILY = 'Roboto';
const FONT_FILE_REGULAR = 'Roboto-Regular.ttf';
const FONT_FILE_BOLD = 'Roboto-Bold.ttf';

const MONTH_NAMES: Record<string, number> = {
  january: 1, february: 2, march: 3, april: 4, may: 5, june: 6,
  july: 7, august: 8, september: 9, october: 10, november: 11, december: 12,
};

// "September 2026" -> "tháng 9/2026" — khớp cách BillingService.BuildInvoiceDescription
// diễn đạt trên backend, phòng khi hóa đơn chưa kịp có Description từ CSDL.
export function composeInvoiceDescription(
  planName: string,
  billingMonth: string,
  options: { hasEquipmentCharge?: boolean; hasDeposit?: boolean; hasLateFee?: boolean } = {}
): string {
  const raw = (billingMonth || '').trim();
  const match = raw.match(/^([A-Za-z]+)\s+(\d{4})$/);
  const period = match && MONTH_NAMES[match[1].toLowerCase()]
    ? `tháng ${MONTH_NAMES[match[1].toLowerCase()]}/${match[2]}`
    : raw || new Date().toLocaleDateString('vi-VN', { month: 'numeric', year: 'numeric' });

  const components = ['cước thuê bao'];
  if (options.hasEquipmentCharge) components.push('phí thuê thiết bị');
  if (options.hasDeposit) components.push('tiền cọc thiết bị');
  if (options.hasLateFee) components.push('phí nộp trễ');
  components.push('thuế dịch vụ');

  const plan = planName?.trim() || 'viễn thông';
  return `Hóa đơn cước phí gói cước ${plan} kỳ ${period} (gồm ${components.join(', ')})`;
}

// VFS của jsPDF là theo từng document nên cache chuỗi base64 ở mức module,
// mỗi lần build PDF chỉ cần đăng ký lại vào doc mới.
let fontBase64Cache: Promise<{ regular: string; bold: string }> | null = null;

async function arrayBufferToBase64(buffer: ArrayBuffer): Promise<string> {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  const chunkSize = 0x8000;
  for (let i = 0; i < bytes.length; i += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
  }
  return btoa(binary);
}

async function loadFontBase64(): Promise<{ regular: string; bold: string }> {
  if (!fontBase64Cache) {
    fontBase64Cache = (async () => {
      const [regularBuffer, boldBuffer] = await Promise.all([
        fetch(fontRegularUrl).then((res) => {
          if (!res.ok) throw new Error(`Không tải được font Roboto-Regular.ttf (${res.status})`);
          return res.arrayBuffer();
        }),
        fetch(fontBoldUrl).then((res) => {
          if (!res.ok) throw new Error(`Không tải được font Roboto-Bold.ttf (${res.status})`);
          return res.arrayBuffer();
        }),
      ]);
      return {
        regular: await arrayBufferToBase64(regularBuffer),
        bold: await arrayBufferToBase64(boldBuffer),
      };
    })();
  }
  return fontBase64Cache;
}

function registerFonts(doc: jsPDF, fonts: { regular: string; bold: string }): void {
  doc.addFileToVFS(FONT_FILE_REGULAR, fonts.regular);
  doc.addFont(FONT_FILE_REGULAR, FONT_FAMILY, 'normal');
  doc.addFileToVFS(FONT_FILE_BOLD, fonts.bold);
  doc.addFont(FONT_FILE_BOLD, FONT_FAMILY, 'bold');
}

const money = (value: number): string => `$${(Number.isFinite(value) ? value : 0).toFixed(2)}`;

const formatDateVi = (value: string): string => {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

export function billToInvoicePdfData(bill: Bill): InvoicePdfData {
  const planName = bill.planName || 'Broadband';
  return {
    invoiceNumber: bill.invoiceNumber,
    description:
      bill.description?.trim() ||
      composeInvoiceDescription(planName, bill.billingMonth, {
        hasEquipmentCharge: (bill.hourlyCharges ?? 0) > 0,
        hasDeposit: (bill.securityDeposit ?? 0) > 0,
      }),
    billingPeriod: bill.billingMonth,
    customerName: bill.customerName || 'Nexus Valued Subscriber',
    accountId: bill.accountId,
    planName,
    equipmentName: 'Router',
    planCharge: bill.monthlyRental ?? 0,
    equipmentCharge: bill.hourlyCharges ?? 0,
    discountPercent: bill.discountPercent ?? 0,
    discountAmount: bill.discountAmount ?? 0,
    securityDeposit: bill.securityDeposit ?? 0,
    serviceTaxRate: bill.serviceTaxRate ?? 0,
    serviceTaxAmount: bill.serviceTaxAmount ?? 0,
    totalAmount: bill.totalAmount ?? 0,
    amountPaid: bill.amountPaid ?? 0,
    dueAmount: bill.dueAmount ?? 0,
    generatedBy: 'Accounts Department',
    date: formatDateVi(bill.billingDate),
  };
}

export function buildInvoicePdf(data: InvoicePdfData, fonts: { regular: string; bold: string }): jsPDF {
  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  registerFonts(doc, fonts);
  const pageWidth = doc.internal.pageSize.getWidth();
  const marginX = 20;
  const contentWidth = pageWidth - marginX * 2;
  const labelX = marginX + 4;
  const infoValueX = marginX + 56; // nhãn info dài nhất: "Mã tài khoản (Account ID)"
  const detailValueX = marginX + 82; // nhãn dài nhất: "Phí thuê thiết bị (Equipment Charge)"

  const setFont = (style: 'normal' | 'bold', size: number) => {
    doc.setFont(FONT_FAMILY, style);
    doc.setFontSize(size);
  };

  const dashedLine = (y: number) => {
    doc.setLineDashPattern([1.2, 1.2], 0);
    doc.setLineWidth(0.2);
    doc.setDrawColor(90, 90, 90);
    doc.line(marginX, y, marginX + contentWidth, y);
    doc.setLineDashPattern([], 0);
  };

  // ---- Header + mô tả hóa đơn "của cái gì" ----
  setFont('bold', 17);
  doc.text('NEXUS COMMUNICATION SYSTEM', pageWidth / 2, 26, { align: 'center' });
  setFont('bold', 13);
  doc.text('I N V O I C E', pageWidth / 2, 33.5, { align: 'center' });

  let y = 40.5;
  setFont('bold', 11);
  const descriptionLines = doc.splitTextToSize(data.description, contentWidth - 6) as string[];
  descriptionLines.forEach((line) => {
    doc.text(line, pageWidth / 2, y, { align: 'center' });
    y += 5.8;
  });

  setFont('normal', 9);
  doc.setTextColor(110, 110, 110);
  doc.text(
    `Invoice for ${data.planName} service plan - billing period ${data.billingPeriod}`,
    pageWidth / 2,
    y,
    { align: 'center' }
  );
  doc.setTextColor(0, 0, 0);

  y += 4.5;
  doc.setLineWidth(0.5);
  doc.setDrawColor(30, 30, 30);
  doc.line(marginX, y, marginX + contentWidth, y);
  y += 9;

  // ---- Thông tin hóa đơn (nhãn song ngữ Việt - Anh) ----
  const infoRow = (label: string, value: string) => {
    setFont('bold', 10.5);
    doc.text(label, labelX, y);
    setFont('normal', 10.5);
    doc.text(value, infoValueX, y);
    y += 6.8;
  };
  infoRow('Số hóa đơn (Invoice ID)', data.invoiceNumber);
  infoRow('Khách hàng (Customer)', data.customerName);
  infoRow('Mã tài khoản (Account ID)', data.accountId);

  y += 1.5;
  dashedLine(y);
  y += 8.5;

  // ---- Chi tiết các khoản ----
  const detailRow = (label: string, value: string, bold = false) => {
    setFont('bold', 10.5);
    doc.text(label, labelX, y);
    setFont(bold ? 'bold' : 'normal', 10.5);
    doc.text(value, detailValueX, y);
    y += 6.8;
  };

  detailRow('Gói cước (Plan)', data.planName);
  detailRow('Thiết bị (Equipment)', data.equipmentName);
  detailRow('Cước gói cước (Plan Charge)', money(data.planCharge));
  detailRow('Phí thuê thiết bị (Equipment Charge)', money(data.equipmentCharge));
  detailRow(
    'Giảm giá (Discount)',
    data.discountAmount > 0
      ? `${data.discountPercent}% (-${money(data.discountAmount)})`
      : `${data.discountPercent}%`
  );
  detailRow('Tiền cọc (Security Deposit)', money(data.securityDeposit));
  detailRow(
    'Thuế dịch vụ (Service Tax)',
    data.serviceTaxAmount > 0
      ? `${data.serviceTaxRate}% (${money(data.serviceTaxAmount)})`
      : `${data.serviceTaxRate}%`
  );

  y += 1.5;
  dashedLine(y);
  y += 8.5;

  detailRow('Tổng cộng (Total Amount)', money(data.totalAmount), true);
  detailRow('Đã thanh toán (Paid)', money(data.amountPaid));
  detailRow('Còn phải trả (Due)', money(data.dueAmount), true);

  y += 1.5;
  dashedLine(y);
  y += 9;

  detailRow('Nơi phát hành (Generated by)', data.generatedBy);
  detailRow('Ngày phát hành (Date)', data.date);

  // ---- Footer ----
  setFont('normal', 8.5);
  doc.setTextColor(120, 120, 120);
  doc.text(
    `NEXUS Communication System - Invoice ${data.invoiceNumber}`,
    pageWidth / 2,
    doc.internal.pageSize.getHeight() - 14,
    { align: 'center' }
  );

  return doc;
}

export async function exportInvoicePdf(data: InvoicePdfData, filename: string): Promise<void> {
  const fonts = await loadFontBase64();
  const doc = buildInvoicePdf(data, fonts);
  doc.save(filename);
}
