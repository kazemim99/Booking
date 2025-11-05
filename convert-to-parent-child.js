const fs = require('fs');

// Read the original data
const data = JSON.parse(fs.readFileSync('./ProvinceCity.json', 'utf8'));

// Filter only provinces and cities
const provinces = data.filter(x => x.CODEREC === 1);
const cities = data.filter(x => x.CODEREC === 2);

// Create the parent-child structure
const result = [];
let currentId = 1;

// Add provinces (parent items with ParentId = null)
provinces.forEach(province => {
  const provinceId = currentId++;

  result.push({
    Id: provinceId,
    Name: province['نام استان'],
    ProvinceCode: province['کد استان'],
    ParentId: null,
    Type: 'Province'
  });

  // Add cities for this province (children with ParentId = province Id)
  const provinceCities = cities.filter(city =>
    city['کد استان'] === province['کد استان']
  );

  provinceCities.forEach(city => {
    result.push({
      Id: currentId++,
      Name: city['نام شهرستان'] || city['نام'],
      ProvinceCode: city['کد استان'],
      CityCode: city['کد شهرستان'],
      ParentId: provinceId,
      Type: 'City'
    });
  });
});

// Save the result
fs.writeFileSync('./ProvinceCity-ParentChild.json', JSON.stringify(result, null, 2), 'utf8');

console.log('✅ Conversion complete!');
console.log(`📊 Total records: ${result.length}`);
console.log(`📍 Provinces: ${provinces.length}`);
console.log(`🏙️  Cities: ${cities.length}`);
console.log(`📁 Output file: ProvinceCity-ParentChild.json`);
