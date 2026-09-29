import fs from 'fs';
import path from 'path';

const seedSqlPath = path.resolve('scripts/sql/2026-09-28_seed_orders_10_per_plan.sql');
const dbSqlPath = path.resolve('frontend/NexusSystem_Database.sql');

const seedContent = fs.readFileSync(seedSqlPath, 'utf-8');
const dbContent = fs.readFileSync(dbSqlPath, 'utf-8');

const marker = '-- 5.16. SEED ORDERS FOR ALL 17 PLANS (10 ORDERS PER PLAN)';
if (dbContent.includes(marker)) {
  console.log('Seed content already exists in NexusSystem_Database.sql');
} else {
  // Strip initial USE NexusSystem; GO from seedContent
  const strippedSeed = seedContent.replace(/USE NexusSystem;\s*GO\s*/i, '');
  const updatedDb = dbContent.trimEnd() + '\n\n' + marker + '\n' + strippedSeed + '\n';
  fs.writeFileSync(dbSqlPath, updatedDb, 'utf-8');
  console.log('Appended seed orders to frontend/NexusSystem_Database.sql successfully.');
}
